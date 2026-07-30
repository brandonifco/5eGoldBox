using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

/// Turns a scenario's authored opposition into encounter participants.
///
/// The party goes through character resolution, which needs a race, a class
/// and a background. The opposition has none of those - it is a stat block -
/// so it declares its modifiers directly and its weapons resolve from the
/// ruleset the same way anyone else's do.
internal static class ScenarioCombatantMapper
{
    internal static ScenarioEncounterCombatant CreateParticipant(
        EncounterCombatantDefinition placement,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(ruleset);

        MonsterDefinition monster = ruleset.Definition.Monsters
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                placement.MonsterId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Combatant '{placement.CombatantId}' references monster '{placement.MonsterId}', which the ruleset does not define.");

        IReadOnlyDictionary<Ability, int> modifiers =
            CreateModifierLookup(placement, monster);

        return new ScenarioEncounterCombatant(
            new EncounterParticipantSetup
            {
                Combatant = CombatantRules.Create(
                    placement.CombatantId,
                    monster.MaximumHitPoints,
                    monster.ZeroHitPointPolicy),
                CombatProfile = new EncounterCombatProfile
                {
                    ArmorClass = monster.ArmorClass,
                    WeaponAttacks = monster.Weapons
                        .Select(weapon => CreateWeaponAttack(
                            placement,
                            monster,
                            weapon,
                            modifiers,
                            ruleset))
                        .ToArray(),
                    SavingThrowBonuses =
                        CreateSavingThrowBonuses(modifiers),
                    DamageResponses =
                        Array.Empty<CharacterDamageResponse>()
                },
                SideId = placement.SideId,
                MovementSpeedFeet = monster.MovementSpeedFeet,
                StartingPosition = placement.StartingPosition
            },
            // Initiative is a Dexterity check, so it uses the same modifier
            // everything else Dexterity-based does.
            InitiativeBonus: modifiers[Ability.Dexterity]);
    }

    private static IReadOnlyDictionary<Ability, int> CreateModifierLookup(
        EncounterCombatantDefinition placement,
        MonsterDefinition monster)
    {
        Dictionary<Ability, int> modifiers = new();

        foreach (MonsterAbilityModifier modifier
            in monster.AbilityModifiers)
        {
            if (!modifiers.TryAdd(modifier.Ability, modifier.Modifier))
            {
                throw new InvalidOperationException(
                    $"Combatant '{placement.CombatantId}' declares ability '{modifier.Ability}' twice.");
            }
        }

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!modifiers.ContainsKey(ability))
            {
                throw new InvalidOperationException(
                    $"Combatant '{placement.CombatantId}' declares no modifier for ability '{ability}'.");
            }
        }

        return modifiers;
    }

    private static WeaponAttack CreateWeaponAttack(
        EncounterCombatantDefinition placement,
        MonsterDefinition monster,
        MonsterWeaponDefinition authored,
        IReadOnlyDictionary<Ability, int> modifiers,
        ValidatedRuleset ruleset)
    {
        WeaponDefinition weapon = ruleset.Definition.Weapons
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                authored.WeaponId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Combatant '{placement.CombatantId}' carries weapon '{authored.WeaponId}', which the ruleset does not define.");

        Ability attackAbility = ResolveAttackAbility(weapon);
        int abilityModifier = modifiers[attackAbility];
        int proficiencyBonus = monster.IsProficientWithWeapons
            ? monster.ProficiencyBonus
            : 0;

        return new WeaponAttack
        {
            WeaponId = weapon.Id,
            WeaponName = weapon.Name,
            Category = weapon.Category,
            AttackKind = weapon.AttackKind,
            AttackAbility = attackAbility,
            AbilityModifier = abilityModifier,
            IsProficient = monster.IsProficientWithWeapons,
            ProficiencyBonus = proficiencyBonus,
            AttackBonus = abilityModifier + proficiencyBonus,
            HasDisadvantage = false,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = weapon.Damage,
            VersatileDamage = weapon.VersatileDamage,
            DamageType = weapon.DamageType,
            DamageBonus = abilityModifier,
            Properties = weapon.Properties,
            ReachFeet = weapon.ReachFeet,
            NormalRangeFeet = weapon.NormalRangeFeet,
            LongRangeFeet = weapon.LongRangeFeet,
            AmmunitionItemId = authored.AmmunitionItemId,
            AmmunitionQuantityAvailable = authored.AmmunitionQuantity
        };
    }

    /// Ranged weapons use Dexterity, and a finesse weapon may. The opposition
    /// declares one modifier per ability rather than a choice, so finesse
    /// resolves to Dexterity here rather than to whichever is higher.
    private static Ability ResolveAttackAbility(
        WeaponDefinition weapon)
    {
        return weapon.AttackKind == WeaponAttackKind.Ranged
            || weapon.Properties.Contains(
                RuleIds.WeaponProperties.Finesse,
                StringComparer.Ordinal)
            ? Ability.Dexterity
            : Ability.Strength;
    }

    private static IReadOnlyList<SavingThrowBonus> CreateSavingThrowBonuses(
        IReadOnlyDictionary<Ability, int> modifiers)
    {
        return Enum.GetValues<Ability>()
            .Select(ability => new SavingThrowBonus
            {
                Ability = ability,
                AbilityModifier = modifiers[ability],
                IsProficient = false,
                ProficiencyBonus = 0,
                TotalBonus = modifiers[ability]
            })
            .ToArray();
    }
}
