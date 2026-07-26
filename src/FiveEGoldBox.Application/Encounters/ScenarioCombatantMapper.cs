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
        CombatantDefinition combatant,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(combatant);
        ArgumentNullException.ThrowIfNull(ruleset);

        IReadOnlyDictionary<Ability, int> modifiers =
            CreateModifierLookup(combatant);

        return new ScenarioEncounterCombatant(
            new EncounterParticipantSetup
            {
                Combatant = CombatantRules.Create(
                    combatant.CombatantId,
                    combatant.MaximumHitPoints,
                    combatant.ZeroHitPointPolicy),
                CombatProfile = new EncounterCombatProfile
                {
                    ArmorClass = combatant.ArmorClass,
                    WeaponAttacks = combatant.Weapons
                        .Select(weapon => CreateWeaponAttack(
                            combatant,
                            weapon,
                            modifiers,
                            ruleset))
                        .ToArray(),
                    SavingThrowBonuses =
                        CreateSavingThrowBonuses(modifiers),
                    DamageResponses =
                        Array.Empty<CharacterDamageResponse>()
                },
                SideId = combatant.SideId,
                MovementSpeedFeet = combatant.MovementSpeedFeet,
                StartingPosition = combatant.StartingPosition
            },
            // Initiative is a Dexterity check, so it uses the same modifier
            // everything else Dexterity-based does.
            InitiativeBonus: modifiers[Ability.Dexterity]);
    }

    private static IReadOnlyDictionary<Ability, int> CreateModifierLookup(
        CombatantDefinition combatant)
    {
        Dictionary<Ability, int> modifiers = new();

        foreach (CombatantAbilityModifier modifier
            in combatant.AbilityModifiers)
        {
            if (!modifiers.TryAdd(modifier.Ability, modifier.Modifier))
            {
                throw new InvalidOperationException(
                    $"Combatant '{combatant.CombatantId}' declares ability '{modifier.Ability}' twice.");
            }
        }

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            if (!modifiers.ContainsKey(ability))
            {
                throw new InvalidOperationException(
                    $"Combatant '{combatant.CombatantId}' declares no modifier for ability '{ability}'.");
            }
        }

        return modifiers;
    }

    private static WeaponAttack CreateWeaponAttack(
        CombatantDefinition combatant,
        CombatantWeaponDefinition authored,
        IReadOnlyDictionary<Ability, int> modifiers,
        ValidatedRuleset ruleset)
    {
        WeaponDefinition weapon = ruleset.Definition.Weapons
            .FirstOrDefault(candidate => string.Equals(
                candidate.Id,
                authored.WeaponId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Combatant '{combatant.CombatantId}' carries weapon '{authored.WeaponId}', which the ruleset does not define.");

        Ability attackAbility = ResolveAttackAbility(weapon);
        int abilityModifier = modifiers[attackAbility];
        int proficiencyBonus = combatant.IsProficientWithWeapons
            ? combatant.ProficiencyBonus
            : 0;

        return new WeaponAttack
        {
            WeaponId = weapon.Id,
            WeaponName = weapon.Name,
            Category = weapon.Category,
            AttackKind = weapon.AttackKind,
            AttackAbility = attackAbility,
            AbilityModifier = abilityModifier,
            IsProficient = combatant.IsProficientWithWeapons,
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
