using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterWeaponAttackPrerequisiteRulesTests
{
    [Fact]
    public void Evaluate_RangedAttackWithAdjacentConsciousHostile_IsLegalWithDisadvantage()
    {
        EncounterState state = CreateEncounter(
            includeAdjacentThreat: true);

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Disadvantage, result.AttackRollMode);
    }

    [Fact]
    public void DiscoverWeaponAttacks_AdjacentHostileRangedCandidateRemainsLegal()
    {
        EncounterState state = CreateEncounter(
            includeAdjacentThreat: true);

        EncounterActionDiscoveryResult result =
            EncounterActionDiscoveryRules.DiscoverWeaponAttacks(
                state,
                [
                    new EncounterWeaponAttackDiscoveryCandidate
                    {
                        ActionOptionId = "attack.test",
                        ActorCombatantId = "combatant.actor",
                        TargetCombatantId = "combatant.target",
                        WeaponId = "weapon.test"
                    }
                ]);

        EncounterActionEvaluation evaluation = Assert.Single(result.Evaluations);
        Assert.True(evaluation.IsCommonlyLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            evaluation.UnavailabilityReason);
    }

    [Fact]
    public void Evaluate_RangedAttackWithoutAdjacentHostile_PreservesConfiguredMode()
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Normal, result.AttackRollMode);
    }

    [Fact]
    public void Evaluate_AdjacentAlly_DoesNotCauseDisadvantage()
    {
        EncounterState state = CreateEncounter(
            includeAdjacentAlly: true);

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Normal, result.AttackRollMode);
    }

    [Theory]
    [InlineData(CombatantLifecycleState.Dying)]
    [InlineData(CombatantLifecycleState.Stable)]
    [InlineData(CombatantLifecycleState.Dead)]
    [InlineData(CombatantLifecycleState.Defeated)]
    public void Evaluate_AdjacentNonconsciousHostile_DoesNotCauseDisadvantage(
        CombatantLifecycleState lifecycle)
    {
        EncounterState state = CreateEncounter(
            includeAdjacentThreat: true);
        state = ReplaceCombatantLifecycle(
            state,
            "combatant.threat",
            lifecycle);

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Normal, result.AttackRollMode);
    }

    [Fact]
    public void Evaluate_HostileBeyondFiveFeet_DoesNotCauseAdjacentDisadvantage()
    {
        EncounterState state = CreateEncounter(
            includeAdjacentThreat: true,
            threatPosition: new GridPosition(3, 1));

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Normal, result.AttackRollMode);
    }

    [Fact]
    public void Evaluate_MeleeWeapon_IsUnaffectedByAdjacentHostility()
    {
        EncounterState state = CreateEncounter(
            weapon: CreateWeapon(
                WeaponAttackKind.Melee,
                D20RollMode.Normal,
                reachFeet: 5),
            targetPosition: new GridPosition(2, 1),
            includeAdjacentThreat: true);

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(D20RollMode.Normal, result.AttackRollMode);
    }

    [Theory]
    [InlineData(D20RollMode.Advantage, false, true, D20RollMode.Normal)]
    [InlineData(D20RollMode.Disadvantage, false, true, D20RollMode.Disadvantage)]
    [InlineData(D20RollMode.Normal, true, true, D20RollMode.Disadvantage)]
    [InlineData(D20RollMode.Advantage, true, true, D20RollMode.Normal)]
    public void Evaluate_AggregatesConfiguredLongRangeAndAdjacentSources(
        D20RollMode configuredMode,
        bool useLongRange,
        bool includeAdjacentThreat,
        D20RollMode expectedMode)
    {
        WeaponAttack weapon = CreateWeapon(
            WeaponAttackKind.Ranged,
            configuredMode,
            normalRangeFeet: 20,
            longRangeFeet: 120,
            ammunitionQuantity: 10);
        GridPosition targetPosition = useLongRange
            ? new GridPosition(6, 1)
            : new GridPosition(4, 1);
        EncounterState state = CreateEncounter(
            weapon,
            targetPosition,
            includeAdjacentThreat);

        EncounterWeaponAttackPrerequisiteEvaluation result = Evaluate(state);

        Assert.True(result.IsLegal);
        Assert.Equal(expectedMode, result.AttackRollMode);
    }

    private static EncounterWeaponAttackPrerequisiteEvaluation Evaluate(
        EncounterState state)
    {
        return EncounterWeaponAttackPrerequisiteRules.Evaluate(
            state,
            "combatant.actor",
            "combatant.target",
            "weapon.test");
    }

    private static EncounterState CreateEncounter(
        WeaponAttack? weapon = null,
        GridPosition? targetPosition = null,
        bool includeAdjacentThreat = false,
        GridPosition? threatPosition = null,
        bool includeAdjacentAlly = false)
    {
        WeaponAttack resolvedWeapon = weapon ?? CreateWeapon(
            WeaponAttackKind.Ranged,
            D20RollMode.Normal,
            normalRangeFeet: 80,
            longRangeFeet: 320,
            ammunitionQuantity: 10);
        WeaponAttack melee = CreateWeapon(
            WeaponAttackKind.Melee,
            D20RollMode.Normal,
            reachFeet: 5);
        List<EncounterParticipantSetup> participants =
        [
            CreateParticipant(
                "combatant.actor",
                "side.party",
                new GridPosition(1, 1),
                resolvedWeapon),
            CreateParticipant(
                "combatant.target",
                "side.enemy",
                targetPosition ?? new GridPosition(4, 1),
                melee)
        ];

        if (includeAdjacentThreat)
        {
            participants.Add(CreateParticipant(
                "combatant.threat",
                "side.enemy",
                threatPosition ?? new GridPosition(1, 2),
                melee));
        }

        if (includeAdjacentAlly)
        {
            participants.Add(CreateParticipant(
                "combatant.ally",
                "side.party",
                new GridPosition(1, 2),
                melee));
        }

        InitiativeOrderEntry[] order = participants
            .Select((participant, index) => new InitiativeOrderEntry
            {
                CombatantId = participant.Combatant.CombatantId,
                Initiative = InitiativeRules.ResolveInitiative(
                    D20RollMode.Normal,
                    20 - index,
                    null,
                    0),
                Position = index + 1,
                HasTiedInitiative = false
            })
            .ToArray();

        return EncounterRules.Start(
            "encounter.prerequisite",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.prerequisite",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            order);
    }

    private static EncounterParticipantSetup CreateParticipant(
        string id,
        string side,
        GridPosition position,
        WeaponAttack weapon)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                id,
                20,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 14,
                WeaponAttacks = [weapon]
            },
            SideId = side,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static WeaponAttack CreateWeapon(
        WeaponAttackKind kind,
        D20RollMode mode,
        int? reachFeet = null,
        int? normalRangeFeet = null,
        int? longRangeFeet = null,
        int? ammunitionQuantity = null)
    {
        return new WeaponAttack
        {
            WeaponId = "weapon.test",
            WeaponName = "Test Weapon",
            Category = WeaponCategory.Martial,
            AttackKind = kind,
            AttackAbility = Ability.Dexterity,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = mode == D20RollMode.Disadvantage,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = mode,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            VersatileDamage = null,
            DamageType = "damage.piercing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            ReachFeet = reachFeet,
            NormalRangeFeet = normalRangeFeet,
            LongRangeFeet = longRangeFeet,
            AmmunitionItemId = kind == WeaponAttackKind.Ranged
                ? "item.arrow"
                : null,
            AmmunitionQuantityAvailable = kind == WeaponAttackKind.Ranged
                ? ammunitionQuantity
                : null
        };
    }

    private static EncounterState ReplaceCombatantLifecycle(
        EncounterState state,
        string combatantId,
        CombatantLifecycleState lifecycle)
    {
        EncounterParticipantState[] participants = state.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId == combatantId);
        EncounterParticipantState participant = participants[index];
        CombatantZeroHitPointPolicy policy = lifecycle
            == CombatantLifecycleState.Defeated
                ? CombatantZeroHitPointPolicy.Defeated
                : CombatantZeroHitPointPolicy.DeathSavingThrows;
        DeathSavingThrowState saves = lifecycle switch
        {
            CombatantLifecycleState.Stable => new DeathSavingThrowState
            {
                SuccessCount = 0,
                FailureCount = 0,
                IsStable = true
            },
            CombatantLifecycleState.Dead => new DeathSavingThrowState
            {
                SuccessCount = 0,
                FailureCount = 3,
                IsStable = false
            },
            _ => new DeathSavingThrowState
            {
                SuccessCount = 0,
                FailureCount = 0,
                IsStable = false
            }
        };

        participants[index] = participant with
        {
            Combatant = participant.Combatant with
            {
                ZeroHitPointPolicy = policy,
                Health = new CombatantHealthState
                {
                    HitPoints = new HitPointState
                    {
                        MaximumHitPoints = 20,
                        CurrentHitPoints = 0,
                        TemporaryHitPoints = 0
                    },
                    DeathSavingThrows = saves,
                    IsInstantlyDead = false
                }
            }
        };

        return state with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }
}
