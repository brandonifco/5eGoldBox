using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterWeaponAttackEvaluationTests
{
    [Theory]
    [InlineData(1, AttackRollOutcome.Miss)]
    [InlineData(20, AttackRollOutcome.CriticalHit)]
    [InlineData(8, AttackRollOutcome.Miss)]
    [InlineData(10, AttackRollOutcome.Hit)]
    public void Evaluate_ResolvesAuthoritativeAttackOutcome(
        int firstRoll,
        AttackRollOutcome expectedOutcome)
    {
        EncounterState state = CreateEncounter();

        EncounterWeaponAttackEvaluation result = Evaluate(
            state,
            firstRoll);

        Assert.Equal(expectedOutcome, result.AttackRoll.Outcome);
        Assert.Equal(state.Revision, result.EncounterRevision);
    }

    [Fact]
    public void Evaluate_MissRequiresNoDamageDice()
    {
        EncounterWeaponAttackEvaluation result = Evaluate(
            CreateEncounter(),
            firstRoll: 1);

        Assert.Null(result.RequiredDamageDice);
    }

    [Fact]
    public void Evaluate_HitRequiresPrimaryWeaponDamageDice()
    {
        EncounterWeaponAttackEvaluation result = Evaluate(
            CreateEncounter(),
            firstRoll: 10);

        Assert.NotNull(result.RequiredDamageDice);
        Assert.Equal(1, result.RequiredDamageDice.Count);
        Assert.Equal(DieType.D8, result.RequiredDamageDice.Die);
    }

    [Fact]
    public void Evaluate_MeleeWeaponUsesPrimaryDamageRatherThanVersatileDamage()
    {
        EncounterWeaponAttackEvaluation result = Evaluate(
            CreateEncounter(useMeleeWeapon: true),
            firstRoll: 10);

        Assert.NotNull(result.RequiredDamageDice);
        Assert.Equal(1, result.RequiredDamageDice.Count);
        Assert.Equal(DieType.D8, result.RequiredDamageDice.Die);
    }

    [Fact]
    public void Evaluate_CriticalHitDoublesOnlyDamageDiceCount()
    {
        EncounterWeaponAttackEvaluation result = Evaluate(
            CreateEncounter(),
            firstRoll: 20);

        Assert.NotNull(result.RequiredDamageDice);
        Assert.Equal(2, result.RequiredDamageDice.Count);
        Assert.Equal(DieType.D8, result.RequiredDamageDice.Die);
    }

    [Theory]
    [InlineData(D20RollMode.Advantage, 4, 15, 15)]
    [InlineData(D20RollMode.Disadvantage, 4, 15, 4)]
    public void Evaluate_UsesAuthoritativeRollMode(
        D20RollMode mode,
        int firstRoll,
        int secondRoll,
        int expectedNaturalRoll)
    {
        EncounterState state = CreateEncounter(
            attackRollMode: mode);

        EncounterWeaponAttackEvaluation result = Evaluate(
            state,
            firstRoll,
            secondRoll);

        Assert.Equal(mode, result.AttackRoll.RollMode);
        Assert.Equal(expectedNaturalRoll, result.AttackRoll.NaturalRoll);
    }

    [Theory]
    [InlineData(EncounterCoverLevel.Half, 17)]
    [InlineData(EncounterCoverLevel.ThreeQuarters, 20)]
    public void Evaluate_UsesCoverAdjustedArmorClass(
        EncounterCoverLevel coverLevel,
        int expectedArmorClass)
    {
        EncounterState state = CreateEncounter(
            targetPosition: new GridPosition(5, 1),
            coverPosition: new EncounterCoverPosition
            {
                Position = new GridPosition(3, 1),
                CoverLevel = coverLevel
            });

        EncounterWeaponAttackEvaluation result = Evaluate(
            state,
            firstRoll: 10);

        Assert.Equal(
            expectedArmorClass,
            result.AttackRoll.TargetArmorClass);
    }

    [Fact]
    public void Evaluate_DoesNotMutateEncounterStateOrResources()
    {
        EncounterState state = CreateEncounter(
            ammunitionQuantity: 4);
        EncounterParticipantState actorBefore = FindParticipant(
            state,
            "combatant.actor");
        EncounterParticipantState targetBefore = FindParticipant(
            state,
            "combatant.target");

        _ = Evaluate(state, firstRoll: 10);

        Assert.Equal(1, state.Revision);
        Assert.Equal("combatant.actor", state.ActiveCombatantId);
        Assert.Equal(EncounterLifecycleState.Active, state.LifecycleState);
        Assert.Null(state.WinningSideId);
        Assert.Null(state.PendingDeathSavingThrowCombatantId);
        Assert.Equal(actorBefore, FindParticipant(state, "combatant.actor"));
        Assert.Equal(targetBefore, FindParticipant(state, "combatant.target"));
        Assert.True(actorBefore.TurnResources.HasActionAvailable);
        Assert.Equal(
            4,
            Assert.Single(actorBefore.CombatProfile.WeaponAttacks)
                .AmmunitionQuantityAvailable);
    }

    [Fact]
    public void Evaluate_AndResolveShareAttackOutcomeAndDamageShape()
    {
        EncounterState state = CreateEncounter(
            ammunitionQuantity: 4);
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            state,
            firstRoll: 10);

        EncounterWeaponAttackResult resolution =
            EncounterWeaponAttackRules.Resolve(
                state,
                new EncounterWeaponAttackCommand
                {
                    ExpectedRevision = state.Revision,
                    ActorCombatantId = "combatant.actor",
                    TargetCombatantId = "combatant.target",
                    WeaponId = "weapon.test",
                    FirstAttackRoll = 10,
                    SecondAttackRoll = null,
                    DamageRolls = [5]
                });

        Assert.Equal(
            evaluation.AttackRoll,
            resolution.Attack.AttackRoll);
        Assert.Equal(
            evaluation.RequiredDamageDice,
            resolution.Attack.Damage.DamageDice);
        Assert.Equal(1, state.Revision);
        Assert.Equal(2, resolution.State.Revision);
    }

    [Fact]
    public void Evaluate_DoesNotReserveAttackAgainstLaterEncounterRevision()
    {
        EncounterState source = CreateEncounter();
        EncounterWeaponAttackEvaluation evaluation = Evaluate(
            source,
            firstRoll: 10);
        EncounterMovementResult moved = EncounterMovementRules.Resolve(
            source,
            new EncounterMovementCommand
            {
                ExpectedRevision = source.Revision,
                ActorCombatantId = "combatant.actor",
                Path = [new GridPosition(1, 2)]
            });

        Assert.Throws<InvalidOperationException>(() =>
            EncounterWeaponAttackRules.Resolve(
                moved.State,
                new EncounterWeaponAttackCommand
                {
                    ExpectedRevision = evaluation.EncounterRevision,
                    ActorCombatantId = "combatant.actor",
                    TargetCombatantId = "combatant.target",
                    WeaponId = "weapon.test",
                    FirstAttackRoll = 10,
                    SecondAttackRoll = null,
                    DamageRolls = [5]
                }));

        Assert.Equal(source.Revision + 1, moved.State.Revision);
        Assert.True(
            FindParticipant(moved.State, "combatant.actor")
                .TurnResources.HasActionAvailable);
        Assert.Equal(
            10,
            Assert.Single(
                FindParticipant(moved.State, "combatant.actor")
                .CombatProfile.WeaponAttacks)
                .AmmunitionQuantityAvailable);
    }

    [Fact]
    public void Evaluate_WhenPrerequisitesAreIllegal_RejectsWithoutTransition()
    {
        EncounterState state = CreateEncounter(
            targetPosition: new GridPosition(11, 11),
            normalRangeFeet: 10,
            longRangeFeet: 20);

        Assert.Throws<InvalidOperationException>(() =>
            Evaluate(state, firstRoll: 10));

        Assert.Equal(1, state.Revision);
        Assert.True(
            FindParticipant(state, "combatant.actor")
                .TurnResources.HasActionAvailable);
    }

    private static EncounterWeaponAttackEvaluation Evaluate(
        EncounterState state,
        int firstRoll,
        int? secondRoll = null)
    {
        return EncounterWeaponAttackRules.Evaluate(
            state,
            new EncounterWeaponAttackEvaluationCommand
            {
                ExpectedRevision = state.Revision,
                ActorCombatantId = "combatant.actor",
                TargetCombatantId = "combatant.target",
                WeaponId = "weapon.test",
                FirstAttackRoll = firstRoll,
                SecondAttackRoll = secondRoll
            });
    }

    private static EncounterState CreateEncounter(
        D20RollMode attackRollMode = D20RollMode.Normal,
        GridPosition? targetPosition = null,
        EncounterCoverPosition? coverPosition = null,
        int normalRangeFeet = 80,
        int longRangeFeet = 320,
        int? ammunitionQuantity = 10,
        bool useMeleeWeapon = false)
    {
        WeaponAttack weapon = new()
        {
            WeaponId = "weapon.test",
            WeaponName = useMeleeWeapon
                ? "Test Longsword"
                : "Test Longbow",
            Category = WeaponCategory.Martial,
            AttackKind = useMeleeWeapon
                ? WeaponAttackKind.Melee
                : WeaponAttackKind.Ranged,
            AttackAbility = useMeleeWeapon
                ? Ability.Strength
                : Ability.Dexterity,
            AbilityModifier = 3,
            IsProficient = true,
            ProficiencyBonus = 2,
            AttackBonus = 5,
            HasDisadvantage = attackRollMode == D20RollMode.Disadvantage,
            DisadvantageReasons = Array.Empty<string>(),
            AttackRollMode = attackRollMode,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D8
            },
            VersatileDamage = new DamageDice
            {
                Count = 1,
                Die = DieType.D10
            },
            DamageType = useMeleeWeapon
                ? "damage.slashing"
                : "damage.piercing",
            DamageBonus = 3,
            Properties = Array.Empty<string>(),
            ReachFeet = useMeleeWeapon ? 5 : null,
            NormalRangeFeet = useMeleeWeapon ? null : normalRangeFeet,
            LongRangeFeet = useMeleeWeapon ? null : longRangeFeet,
            AmmunitionItemId = useMeleeWeapon ? null : "item.arrow",
            AmmunitionQuantityAvailable = useMeleeWeapon
                ? null
                : ammunitionQuantity
        };
        EncounterParticipantSetup actor = CreateParticipant(
            "combatant.actor",
            "side.party",
            new GridPosition(1, 1),
            weapon,
            armorClass: 15);
        EncounterParticipantSetup target = CreateParticipant(
            "combatant.target",
            "side.enemy",
            targetPosition ?? (useMeleeWeapon
                ? new GridPosition(2, 1)
                : new GridPosition(4, 1)),
            weapon,
            armorClass: 15);

        return EncounterRules.Start(
            "encounter.evaluation",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.evaluation",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = coverPosition is null
                    ? Array.Empty<EncounterCoverPosition>()
                    : [coverPosition],
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            [actor, target],
            [
                CreateInitiative("combatant.actor", 1, 20),
                CreateInitiative("combatant.target", 2, 10)
            ]);
    }

    private static EncounterParticipantSetup CreateParticipant(
        string id,
        string side,
        GridPosition position,
        WeaponAttack weapon,
        int armorClass)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                id,
                20,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = armorClass,
                WeaponAttacks = [weapon]
            },
            SideId = side,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static InitiativeOrderEntry CreateInitiative(
        string id,
        int position,
        int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = id,
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                total,
                null,
                0),
            Position = position,
            HasTiedInitiative = false
        };
    }

    private static EncounterParticipantState FindParticipant(
        EncounterState state,
        string id)
    {
        return Assert.Single(
            state.Participants,
            participant => participant.Combatant.CombatantId == id);
    }
}
