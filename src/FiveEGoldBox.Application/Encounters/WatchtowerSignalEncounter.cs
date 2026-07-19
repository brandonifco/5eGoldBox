using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

internal static class WatchtowerSignalEncounter
{
    internal const string EncounterId =
        "encounter.watchtower-signal-ambush";

    internal const string BattlefieldId =
        "battlefield.watchtower-signal-chamber";

    internal const string PartySideId =
        "side.party";

    internal const string RaiderSideId =
        "side.watchtower-raiders";

    internal const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    internal const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    internal const string MeleeRaiderWeaponId =
        "weapon.watchtower-raider.scimitar";

    internal const string RangedRaiderWeaponId =
        "weapon.watchtower-raider.shortbow";

    internal const int BattlefieldWidth = 5;

    internal const int BattlefieldHeight = 4;

    internal const int InitiativeRollCount = 5;

    private static readonly IReadOnlyList<GridPosition>
        PartyStartingPositions =
        [
            new GridPosition(1, 1),
            new GridPosition(1, 2),
            new GridPosition(0, 2)
        ];

    internal static readonly GridPosition
        MeleeRaiderStartingPosition = new(2, 1);

    internal static readonly GridPosition
        RangedRaiderStartingPosition = new(4, 2);

    internal static EncounterState Create(
        PartyState party,
        ValidatedRuleset ruleset,
        int randomSeed,
        int randomValuesConsumed,
        out int updatedRandomValuesConsumed)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(ruleset);

        if (party.Members.Count
            != PartyStartingPositions.Count)
        {
            throw new InvalidOperationException(
                "The watchtower ambush requires exactly three party participants.");
        }

        List<EncounterParticipantSetup> participants = [];
        List<int> initiativeBonuses = [];

        for (int index = 0;
            index < party.Members.Count;
            index++)
        {
            PartyEncounterParticipant participant =
                PartyEncounterMapper.CreateParticipant(
                    party.Members[index],
                    ruleset,
                    PartySideId,
                    PartyStartingPositions[index]);

            participants.Add(participant.Setup);
            initiativeBonuses.Add(
                participant.InitiativeBonus);
        }

        participants.Add(
            CreateMeleeRaider());
        initiativeBonuses.Add(2);

        participants.Add(
            CreateRangedRaider());
        initiativeBonuses.Add(2);

        IReadOnlyList<int> initiativeRolls =
            ApplicationRandomSequence.GenerateD20Rolls(
                randomSeed,
                randomValuesConsumed,
                InitiativeRollCount,
                out updatedRandomValuesConsumed);

        InitiativeOrderCombatant[] initiativeCombatants =
            participants
                .Select((participant, index) =>
                    new InitiativeOrderCombatant
                    {
                        CombatantId =
                            participant.Combatant.CombatantId,
                        Initiative =
                            InitiativeRules.ResolveInitiative(
                                D20RollMode.Normal,
                                initiativeRolls[index],
                                secondRoll: null,
                                initiativeBonus:
                                    initiativeBonuses[index])
                    })
                .ToArray();

        IReadOnlyList<InitiativeOrderEntry>
            initiativeOrder =
                InitiativeOrderRules.ResolveOrder(
                    initiativeCombatants);

        return EncounterRules.Start(
            EncounterId,
            CreateBattlefield(),
            participants,
            initiativeOrder);
    }

    internal static bool IsAuthoredParticipantId(
        string combatantId,
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(party);

        return party.Members.Any(member =>
                string.Equals(
                    member.PartyMemberId,
                    combatantId,
                    StringComparison.Ordinal))
            || string.Equals(
                combatantId,
                MeleeRaiderId,
                StringComparison.Ordinal)
            || string.Equals(
                combatantId,
                RangedRaiderId,
                StringComparison.Ordinal);
    }

    private static EncounterParticipantSetup
        CreateMeleeRaider()
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                MeleeRaiderId,
                maximumHitPoints: 9,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy.Defeated),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 13,
                WeaponAttacks =
                [
                    CreateMeleeRaiderWeapon()
                ],
                SavingThrowBonuses =
                    CreateRaiderSavingThrowBonuses(),
                DamageResponses =
                    Array.Empty<CharacterDamageResponse>()
            },
            SideId = RaiderSideId,
            MovementSpeedFeet = 30,
            StartingPosition =
                MeleeRaiderStartingPosition
        };
    }

    private static EncounterParticipantSetup
        CreateRangedRaider()
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                RangedRaiderId,
                maximumHitPoints: 8,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy.Defeated),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 13,
                WeaponAttacks =
                [
                    CreateRangedRaiderWeapon()
                ],
                SavingThrowBonuses =
                    CreateRaiderSavingThrowBonuses(),
                DamageResponses =
                    Array.Empty<CharacterDamageResponse>()
            },
            SideId = RaiderSideId,
            MovementSpeedFeet = 30,
            StartingPosition =
                RangedRaiderStartingPosition
        };
    }

    private static WeaponAttack
        CreateMeleeRaiderWeapon()
    {
        return new WeaponAttack
        {
            WeaponId = MeleeRaiderWeaponId,
            WeaponName = "Raider Scimitar",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 2,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 4,
            HasDisadvantage = false,
            DisadvantageReasons =
                Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            VersatileDamage = null,
            DamageType = "damage.slashing",
            DamageBonus = 2,
            Properties =
            [
                RuleIds.WeaponProperties.Finesse
            ],
            ReachFeet = 5,
            NormalRangeFeet = null,
            LongRangeFeet = null,
            AmmunitionItemId = null,
            AmmunitionQuantityAvailable = null
        };
    }

    private static WeaponAttack
        CreateRangedRaiderWeapon()
    {
        return new WeaponAttack
        {
            WeaponId = RangedRaiderWeaponId,
            WeaponName = "Raider Shortbow",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Ranged,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 2,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 4,
            HasDisadvantage = false,
            DisadvantageReasons =
                Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            VersatileDamage = null,
            DamageType = "damage.piercing",
            DamageBonus = 2,
            Properties =
            [
                RuleIds.WeaponProperties.Ammunition
            ],
            ReachFeet = null,
            NormalRangeFeet = 80,
            LongRangeFeet = 320,
            AmmunitionItemId = "item.arrow",
            AmmunitionQuantityAvailable = 12
        };
    }

    private static IReadOnlyList<SavingThrowBonus>
        CreateRaiderSavingThrowBonuses()
    {
        return Enum.GetValues<Ability>()
            .Select(ability =>
            {
                int modifier = ability switch
                {
                    Ability.Strength => 1,
                    Ability.Dexterity => 2,
                    Ability.Constitution => 1,
                    Ability.Intelligence => 0,
                    Ability.Wisdom => 0,
                    Ability.Charisma => 0,
                    _ => throw new InvalidOperationException(
                        "The raider saving-throw ability is unsupported.")
                };

                return new SavingThrowBonus
                {
                    Ability = ability,
                    AbilityModifier = modifier,
                    IsProficient = false,
                    ProficiencyBonus = 0,
                    TotalBonus = modifier
                };
            })
            .ToArray();
    }

    private static EncounterBattlefieldState
        CreateBattlefield()
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = BattlefieldId,
            Width = BattlefieldWidth,
            Height = BattlefieldHeight,
            BlockedPositions =
                Array.Empty<GridPosition>(),
            CoverPositions =
                Array.Empty<EncounterCoverPosition>(),
            DifficultTerrainPositions =
                Array.Empty<GridPosition>()
        };
    }
}
