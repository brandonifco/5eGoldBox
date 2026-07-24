using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

internal static class CombatGenericProjectionTestData
{
    internal const string EncounterId = "encounter.generic-proof";
    internal const string BattlefieldId = "battlefield.generic-proof";
    internal const string ExpeditionSideId = "side.expedition";
    internal const string HostileSideId = "side.hostiles";
    internal const string ScoutId = "combatant.scout";
    internal const string GuardId = "combatant.guard";
    internal const string BruteId = "combatant.brute";
    internal const string ArcherId = "combatant.archer";
    internal const string SpearId = "weapon.spear";
    internal const string ShortbowId = "weapon.shortbow";

    internal static HashSet<string> CreateControlledCombatantIds()
    {
        return new HashSet<string>(
            [ScoutId, GuardId],
            StringComparer.Ordinal);
    }

    internal static EncounterState CreatePlayerEncounter()
    {
        return CreateEncounter(
            scoutWeapons:
            [
                CreateSpear(),
                CreateShortbow(ammunitionQuantity: 4)
            ],
            scoutHasAction: true,
            activePosition: 1);
    }

    internal static EncounterState CreateAutomaticEncounter()
    {
        return CreateEncounter(
            scoutWeapons:
            [
                CreateSpear(),
                CreateShortbow(ammunitionQuantity: 4)
            ],
            scoutHasAction: true,
            activePosition: 2);
    }

    internal static EncounterState CreatePendingDeathSavingThrowEncounter()
    {
        EncounterState state = CreatePlayerEncounter();
        EncounterParticipantState[] participants =
            state.Participants.ToArray();
        int scoutIndex = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                ScoutId,
                StringComparison.Ordinal));
        EncounterParticipantState scout = participants[scoutIndex];
        CombatantDamageResult damage = CombatantRules.ResolveDamage(
            scout.Combatant,
            scout.Combatant.Health.HitPoints.CurrentHitPoints,
            isCriticalHit: false);

        participants[scoutIndex] = scout with
        {
            Combatant = damage.State
        };

        return state with
        {
            Participants = Array.AsReadOnly(participants),
            PendingDeathSavingThrowCombatantId = ScoutId
        };
    }

    internal static EncounterState CreateCompletedEncounter()
    {
        return EncounterRules.Complete(
            CreatePlayerEncounter(),
            HostileSideId);
    }

    internal static EncounterState CreateZeroAmmunitionEncounter()
    {
        return CreateEncounter(
            scoutWeapons:
            [
                CreateSpear(),
                CreateShortbow(ammunitionQuantity: 0)
            ],
            scoutHasAction: true,
            activePosition: 1);
    }

    internal static EncounterState CreateNoWeaponsEncounter()
    {
        return CreateEncounter(
            scoutWeapons: Array.Empty<WeaponAttack>(),
            scoutHasAction: true,
            activePosition: 1);
    }

    internal static EncounterState CreateAllTargetsUnavailableEncounter()
    {
        return CreateEncounter(
            scoutWeapons:
            [
                CreateSpear(),
                CreateShortbow(ammunitionQuantity: 4)
            ],
            scoutHasAction: false,
            activePosition: 1);
    }

    private static EncounterState CreateEncounter(
        IReadOnlyList<WeaponAttack> scoutWeapons,
        bool scoutHasAction,
        int activePosition)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                GuardId,
                ExpeditionSideId,
                new GridPosition(0, 4),
                movementSpeedFeet: 10,
                armorClass: 16,
                [CreateSpear()]),
            CreateParticipant(
                BruteId,
                HostileSideId,
                new GridPosition(2, 2),
                movementSpeedFeet: 10,
                armorClass: 13,
                [CreateSpear()]),
            CreateParticipant(
                ScoutId,
                ExpeditionSideId,
                new GridPosition(1, 2),
                movementSpeedFeet: 10,
                armorClass: 14,
                scoutWeapons),
            CreateParticipant(
                ArcherId,
                HostileSideId,
                new GridPosition(6, 2),
                movementSpeedFeet: 10,
                armorClass: 12,
                [CreateShortbow(ammunitionQuantity: 6)])
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(ScoutId, position: 1, total: 18),
            CreateInitiativeEntry(BruteId, position: 2, total: 16),
            CreateInitiativeEntry(GuardId, position: 3, total: 14),
            CreateInitiativeEntry(ArcherId, position: 4, total: 12)
        ];

        EncounterState state = EncounterRules.Start(
            EncounterId,
            new EncounterBattlefieldState
            {
                BattlefieldId = BattlefieldId,
                Width = 7,
                Height = 5,
                BlockedPositions =
                    Array.AsReadOnly(
                        new[] { new GridPosition(1, 1) }),
                CoverPositions =
                    Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions =
                    Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);

        EncounterParticipantState[] participantStates =
            state.Participants.ToArray();
        int scoutIndex = Array.FindIndex(
            participantStates,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                ScoutId,
                StringComparison.Ordinal));

        participantStates[scoutIndex] =
            participantStates[scoutIndex] with
            {
                TurnResources =
                    participantStates[scoutIndex].TurnResources with
                    {
                        HasActionAvailable = scoutHasAction
                    }
            };

        return state with
        {
            Participants = Array.AsReadOnly(participantStates),
            TurnState = state.TurnState with
            {
                ActivePosition = activePosition
            }
        };
    }

    private static EncounterParticipantSetup CreateParticipant(
        string combatantId,
        string sideId,
        GridPosition position,
        int movementSpeedFeet,
        int armorClass,
        IReadOnlyList<WeaponAttack> weapons)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 20,
                zeroHitPointPolicy:
                    CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = armorClass,
                WeaponAttacks =
                    Array.AsReadOnly(weapons.ToArray()),
                SavingThrowBonuses =
                    Array.Empty<SavingThrowBonus>(),
                DamageResponses =
                    Array.Empty<CharacterDamageResponse>()
            },
            SideId = sideId,
            MovementSpeedFeet = movementSpeedFeet,
            StartingPosition = position
        };
    }

    private static InitiativeOrderEntry CreateInitiativeEntry(
        string combatantId,
        int position,
        int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = new InitiativeRollResult
            {
                RollMode = D20RollMode.Normal,
                FirstRoll = total,
                SecondRoll = null,
                NaturalRoll = total,
                InitiativeBonus = 0,
                Total = total
            },
            Position = position,
            HasTiedInitiative = false
        };
    }

    private static WeaponAttack CreateSpear()
    {
        return new WeaponAttack
        {
            WeaponId = SpearId,
            WeaponName = "Spear",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Melee,
            AttackAbility = Ability.Strength,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = false,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            VersatileDamage = null,
            DamageType = "damage.piercing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            ReachFeet = 5,
            NormalRangeFeet = null,
            LongRangeFeet = null,
            AmmunitionItemId = null,
            AmmunitionQuantityAvailable = null
        };
    }

    private static WeaponAttack CreateShortbow(
        int ammunitionQuantity)
    {
        return new WeaponAttack
        {
            WeaponId = ShortbowId,
            WeaponName = "Shortbow",
            Category = WeaponCategory.Simple,
            AttackKind = WeaponAttackKind.Ranged,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = false,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = D20RollMode.Normal,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            VersatileDamage = null,
            DamageType = "damage.piercing",
            DamageBonus = 3,
            Properties = [RuleIds.WeaponProperties.Ammunition],
            ReachFeet = null,
            NormalRangeFeet = 20,
            LongRangeFeet = 60,
            AmmunitionItemId = "item.arrow.generic-proof",
            AmmunitionQuantityAvailable = ammunitionQuantity
        };
    }
}
