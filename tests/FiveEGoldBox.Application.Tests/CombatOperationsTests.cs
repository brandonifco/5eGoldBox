using System.Reflection;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class CombatOperationsTests
{
    private static readonly GridPosition[] ExpectedMovementDestinations =
    [
        new(0, 0),
        new(1, 0),
        new(2, 0),
        new(3, 0),
        new(0, 1),
        new(2, 1),
        new(3, 1),
        new(0, 2),
        new(3, 2),
        new(0, 3),
        new(1, 3),
        new(2, 3),
        new(3, 3),
        new(1, 4),
        new(2, 4),
        new(3, 4)
    ];

    [Fact]
    public void Query_NullSession_ThrowsWithSessionParameter()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => CombatOperations.Query(null!));

        Assert.Equal("session", exception.ParamName);
    }

    [Fact]
    public void Query_ValidSessionWithoutEncounter_ThrowsInvalidOperationException()
    {
        ApplicationSessionState session =
            WatchtowerScenarioSessionFactory.CreateNew(randomSeed: 41);

        Assert.Throws<InvalidOperationException>(
            () => CombatOperations.Query(session));
    }

    [Fact]
    public void Query_CanonicalEncounter_ProjectsCurrentFactsWithoutMutationOrRandomUse()
    {
        ApplicationSessionState session =
            CreateControlledWatchtowerEncounterSession();
        EncounterState encounter = session.ActiveEncounter!.Encounter;
        ApplicationSessionSourceSnapshot sourceBefore =
            CaptureSessionSource(session);

        CombatView first = CombatOperations.Query(session);

        AssertSessionSourceUnchanged(sourceBefore, session);

        CombatView second = CombatOperations.Query(session);

        AssertSessionSourceUnchanged(sourceBefore, session);
        AssertViewsEquivalent(first, second);
        AssertDecisionAgreesWithView(first);
        AssertDecisionAgreesWithView(second);
        Assert.NotSame(first.Combatants, second.Combatants);
        Assert.NotSame(first.Decision.WeaponAttacks,
            second.Decision.WeaponAttacks);
        Assert.NotSame(first.Decision.Movement!.DestinationOptions,
            second.Decision.Movement!.DestinationOptions);

        Assert.Equal("encounter.watchtower-signal-ambush", first.EncounterId);
        Assert.Equal("battlefield.watchtower-signal-chamber", first.BattlefieldId);
        Assert.Equal(5, first.BattlefieldWidth);
        Assert.Equal(4, first.BattlefieldHeight);
        Assert.Equal(encounter.Revision, first.Revision);
        Assert.Equal(encounter.TurnState.RoundNumber, first.RoundNumber);
        Assert.Equal(encounter.ActiveCombatantId, first.ActiveCombatantId);
        Assert.Equal(EncounterLifecycleState.Active, first.LifecycleState);
        Assert.Equal(5, first.Combatants.Count);
        Assert.Equal(
            encounter.Participants.Select(
                participant => participant.Combatant.CombatantId),
            first.Combatants.Select(combatant => combatant.CombatantId));
        Assert.Equal(CombatDecisionState.PlayerDecisionRequired, first.Decision.State);
        Assert.Null(first.PendingDeathSavingThrowCombatantId);
        Assert.Null(first.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Null(first.WinningSideId);
        Assert.Null(first.Decision.WinningSideId);
        Assert.NotNull(first.Decision.Movement);
        Assert.NotNull(first.Decision.EndTurn);
    }

    [Fact]
    public void Create_PlayerDecision_UsesIdentityControlAndCompleteShape()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreatePlayerEncounter();

        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        Assert.Equal(CombatDecisionState.PlayerDecisionRequired, view.Decision.State);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId, view.ActiveCombatantId);
        Assert.Equal(view.ActiveCombatantId, view.Decision.ActiveCombatantId);
        Assert.Null(view.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.WinningSideId);
        Assert.Null(view.Decision.WinningSideId);
        Assert.NotNull(view.Decision.Movement);
        Assert.NotNull(view.Decision.EndTurn);
        Assert.True(view.Decision.EndTurn.IsAvailable);
        Assert.Equal(EncounterActionUnavailabilityReason.None,
            view.Decision.EndTurn.UnavailabilityReason);
        Assert.Equal(view.Revision, view.Decision.EncounterRevision);
        AssertDecisionAgreesWithView(view);
    }

    [Fact]
    public void Create_AutomaticDecision_HasAutomaticOnlyShape()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreateAutomaticEncounter();
        EncounterSourceSnapshot sourceBefore =
            CaptureEncounterSource(encounter);

        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        AssertEncounterSourceUnchanged(sourceBefore, encounter);
        Assert.Equal(CombatDecisionState.AutomaticProcessingRequired, view.Decision.State);
        Assert.Equal(CombatGenericProjectionTestData.BruteId, view.ActiveCombatantId);
        Assert.Equal(view.ActiveCombatantId, view.Decision.ActiveCombatantId);
        Assert.Null(view.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.WinningSideId);
        Assert.Null(view.Decision.WinningSideId);
        Assert.Null(view.Decision.Movement);
        Assert.Empty(view.Decision.WeaponAttacks);
        Assert.Null(view.Decision.EndTurn);
        AssertDecisionAgreesWithView(view);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatWeaponAttackOption>)view.Decision.WeaponAttacks)
                .Add(null!));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);
    }

    [Fact]
    public void Create_ControlledPendingDeathSavingThrow_RequiresAutomaticProcessingWithoutMutation()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData
                .CreatePendingDeathSavingThrowEncounter();
        EncounterSourceSnapshot sourceBefore =
            CaptureEncounterSource(encounter);
        EncounterParticipantState scout = Assert.Single(
            encounter.Participants,
            participant => participant.Combatant.CombatantId
                == CombatGenericProjectionTestData.ScoutId);

        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            encounter.ActiveCombatantId);
        Assert.Equal(CombatantLifecycleState.Dying,
            scout.Combatant.LifecycleState);
        Assert.Equal(0,
            scout.Combatant.Health.HitPoints.CurrentHitPoints);
        Assert.False(scout.Combatant.Health.DeathSavingThrows.IsStable);
        Assert.False(scout.Combatant.Health.IsDead);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            encounter.PendingDeathSavingThrowCombatantId);

        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        AssertEncounterSourceUnchanged(sourceBefore, encounter);
        Assert.Equal(CombatDecisionState.AutomaticProcessingRequired,
            view.Decision.State);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            view.ActiveCombatantId);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            view.Decision.ActiveCombatantId);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            view.PendingDeathSavingThrowCombatantId);
        Assert.Equal(CombatGenericProjectionTestData.ScoutId,
            view.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.Decision.Movement);
        Assert.Empty(view.Decision.WeaponAttacks);
        Assert.Null(view.Decision.EndTurn);
        Assert.Null(view.WinningSideId);
        Assert.Null(view.Decision.WinningSideId);
        AssertDecisionAgreesWithView(view);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatWeaponAttackOption>)view.Decision.WeaponAttacks)
                .Add(null!));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);
    }

    [Fact]
    public void Create_CompletedDecision_HasCompletedOnlyShapeAndNoActiveActor()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreateCompletedEncounter();
        EncounterSourceSnapshot sourceBefore =
            CaptureEncounterSource(encounter);

        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        AssertEncounterSourceUnchanged(sourceBefore, encounter);
        Assert.Equal(EncounterLifecycleState.Completed, view.LifecycleState);
        Assert.Null(view.ActiveCombatantId);
        Assert.Null(view.PendingDeathSavingThrowCombatantId);
        Assert.Equal(CombatGenericProjectionTestData.HostileSideId, view.WinningSideId);
        Assert.Equal(CombatDecisionState.CombatCompleted, view.Decision.State);
        Assert.Null(view.Decision.ActiveCombatantId);
        Assert.Null(view.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Null(view.Decision.Movement);
        Assert.Empty(view.Decision.WeaponAttacks);
        Assert.Null(view.Decision.EndTurn);
        Assert.Equal(view.WinningSideId, view.Decision.WinningSideId);
        Assert.Equal(view.Revision, view.Decision.EncounterRevision);
        AssertDecisionAgreesWithView(view);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatWeaponAttackOption>)view.Decision.WeaponAttacks)
                .Add(null!));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);
    }

    [Fact]
    public void Create_AlternateEncounter_ProjectsParticipantOrderAndCombatantFacts()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreatePlayerEncounter();

        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        Assert.Equal(CombatGenericProjectionTestData.EncounterId, view.EncounterId);
        Assert.Equal(CombatGenericProjectionTestData.BattlefieldId, view.BattlefieldId);
        Assert.Equal(7, view.BattlefieldWidth);
        Assert.Equal(5, view.BattlefieldHeight);
        Assert.Equal(
            new[]
            {
                CombatGenericProjectionTestData.GuardId,
                CombatGenericProjectionTestData.BruteId,
                CombatGenericProjectionTestData.ScoutId,
                CombatGenericProjectionTestData.ArcherId
            },
            view.Combatants.Select(combatant => combatant.CombatantId));

        CombatantView scout = Assert.Single(
            view.Combatants.Where(combatant =>
                combatant.CombatantId == CombatGenericProjectionTestData.ScoutId));
        Assert.Equal(CombatGenericProjectionTestData.ExpeditionSideId, scout.SideId);
        Assert.Equal(new GridPosition(1, 2), scout.Position);
        Assert.Equal(CombatantLifecycleState.Conscious, scout.LifecycleState);
        Assert.Equal(14, scout.ArmorClass);
        Assert.Equal(10, scout.MovementSpeedFeet);
        Assert.Equal(0, scout.MovementSpentFeet);
        Assert.Equal(10, scout.MovementRemainingFeet);
        Assert.True(scout.HasActionAvailable);
        Assert.True(scout.HasBonusActionAvailable);
        Assert.True(scout.HasReactionAvailable);
    }

    [Fact]
    public void Create_MultipleWeapons_ProjectsIndependentTargetMatrixInStableOrder()
    {
        CombatView view = CreateGenericPlayerView();

        Assert.Equal(
            new[]
            {
                CombatGenericProjectionTestData.SpearId,
                CombatGenericProjectionTestData.ShortbowId
            },
            view.Decision.WeaponAttacks.Select(option => option.WeaponId));

        CombatWeaponAttackOption spear = view.Decision.WeaponAttacks[0];
        CombatWeaponAttackOption shortbow = view.Decision.WeaponAttacks[1];

        Assert.True(spear.IsAvailable);
        Assert.Equal(
            new[]
            {
                CombatGenericProjectionTestData.BruteId,
                CombatGenericProjectionTestData.ArcherId
            },
            spear.Targets.Select(target => target.TargetCombatantId));
        Assert.True(spear.Targets[0].IsAvailable);
        Assert.Equal(EncounterActionUnavailabilityReason.None,
            spear.Targets[0].UnavailabilityReason);
        Assert.Equal(D20RollMode.Normal, spear.Targets[0].AttackRollMode);
        Assert.Equal(5, spear.Targets[0].DistanceFeet);
        Assert.False(spear.Targets[1].IsAvailable);
        Assert.Equal(EncounterActionUnavailabilityReason.TargetOutOfRange,
            spear.Targets[1].UnavailabilityReason);
        Assert.Null(spear.Targets[1].AttackRollMode);
        Assert.Equal(25, spear.Targets[1].DistanceFeet);

        Assert.True(shortbow.IsAvailable);
        Assert.Equal(
            new[]
            {
                CombatGenericProjectionTestData.BruteId,
                CombatGenericProjectionTestData.ArcherId
            },
            shortbow.Targets.Select(target => target.TargetCombatantId));
        Assert.All(shortbow.Targets, target => Assert.True(target.IsAvailable));
        Assert.All(shortbow.Targets, target =>
            Assert.Equal(D20RollMode.Disadvantage, target.AttackRollMode));
        Assert.Equal(5, shortbow.Targets[0].DistanceFeet);
        Assert.Equal(25, shortbow.Targets[1].DistanceFeet);
    }

    [Fact]
    public void Create_ZeroAmmunition_PreservesLegalSpearAndUnavailableShortbowTargets()
    {
        CombatView view = CombatViewFactory.Create(
            CombatGenericProjectionTestData.CreateZeroAmmunitionEncounter(),
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        CombatWeaponAttackOption spear = view.Decision.WeaponAttacks[0];
        CombatWeaponAttackOption shortbow = view.Decision.WeaponAttacks[1];

        Assert.True(spear.IsAvailable);
        Assert.True(spear.Targets[0].IsAvailable);
        Assert.False(shortbow.IsAvailable);
        Assert.All(shortbow.Targets, target =>
        {
            Assert.False(target.IsAvailable);
            Assert.Equal(
                EncounterActionUnavailabilityReason.AmmunitionUnavailable,
                target.UnavailabilityReason);
            Assert.Null(target.AttackRollMode);
        });
    }

    [Fact]
    public void Create_NoWeapons_ReturnsProtectedEmptyWeaponCollection()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreateNoWeaponsEncounter();
        EncounterSourceSnapshot sourceBefore =
            CaptureEncounterSource(encounter);
        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        AssertEncounterSourceUnchanged(sourceBefore, encounter);
        Assert.Empty(view.Decision.WeaponAttacks);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatWeaponAttackOption>)view.Decision.WeaponAttacks)
                .Add(null!));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);
    }

    [Fact]
    public void Create_AllTargetsUnavailable_PreservesWeaponsTargetsOrderAndReasons()
    {
        CombatView view = CombatViewFactory.Create(
            CombatGenericProjectionTestData.CreateAllTargetsUnavailableEncounter(),
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        Assert.Equal(2, view.Decision.WeaponAttacks.Count);
        Assert.All(view.Decision.WeaponAttacks, weapon =>
        {
            Assert.False(weapon.IsAvailable);
            Assert.Equal(
                new[]
                {
                    CombatGenericProjectionTestData.BruteId,
                    CombatGenericProjectionTestData.ArcherId
                },
                weapon.Targets.Select(target => target.TargetCombatantId));
            Assert.All(weapon.Targets, target =>
            {
                Assert.False(target.IsAvailable);
                Assert.Equal(
                    EncounterActionUnavailabilityReason.ActionUnavailable,
                    target.UnavailabilityReason);
            });
        });
    }

    [Fact]
    public void Create_MovementOptions_MatchFixedOracleAndCoreAcceptsEveryPath()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreatePlayerEncounter();
        CombatView view = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());
        CombatMovementOption movement = view.Decision.Movement!;

        Assert.True(movement.IsAvailable);
        Assert.Equal(10, movement.MovementRemainingFeet);
        Assert.Equal(
            ExpectedMovementDestinations,
            movement.DestinationOptions.Select(option => option.Destination));
        Assert.Equal(
            movement.DestinationOptions.Count,
            movement.DestinationOptions
                .Select(option => option.Destination)
                .Distinct()
                .Count());

        Assert.Equal(
            new[] { new GridPosition(0, 1), new GridPosition(0, 0) },
            FindDestination(movement, new GridPosition(0, 0)).Path);
        Assert.Equal(
            new[] { new GridPosition(0, 1) },
            FindDestination(movement, new GridPosition(0, 1)).Path);
        Assert.Equal(
            new[] { new GridPosition(2, 1), new GridPosition(3, 2) },
            FindDestination(movement, new GridPosition(3, 2)).Path);

        foreach (CombatMovementDestinationOption option
            in movement.DestinationOptions)
        {
            Assert.NotEmpty(option.Path);
            Assert.DoesNotContain(new GridPosition(1, 2), option.Path);
            Assert.Equal(option.Destination, option.Path[^1]);
            Assert.InRange(option.Destination.X, 0, 6);
            Assert.InRange(option.Destination.Y, 0, 4);
            Assert.DoesNotContain(new GridPosition(1, 1), option.Path);
            Assert.DoesNotContain(new GridPosition(0, 4), option.Path);
            Assert.DoesNotContain(new GridPosition(2, 2), option.Path);
            Assert.DoesNotContain(new GridPosition(6, 2), option.Path);
            Assert.True(option.MovementCostFeet > 0);
            Assert.True(option.MovementCostFeet <= movement.MovementRemainingFeet);

            EncounterMovementResult result = EncounterMovementRules.Resolve(
                encounter,
                new EncounterMovementCommand
                {
                    ExpectedRevision = encounter.Revision,
                    ActorCombatantId = CombatGenericProjectionTestData.ScoutId,
                    Path = option.Path
                });

            Assert.Equal(option.Destination, result.EndingPosition);
            Assert.Equal(option.MovementCostFeet, result.MovementSpentFeet);
        }
    }

    [Fact]
    public void Create_RepeatedProjection_IsPureAndCollectionsAreIndependentAndProtected()
    {
        EncounterState encounter =
            CombatGenericProjectionTestData.CreatePlayerEncounter();
        HashSet<string> controlled =
            CombatGenericProjectionTestData.CreateControlledCombatantIds();
        EncounterSourceSnapshot sourceBefore =
            CaptureEncounterSource(encounter);

        CombatView first = CombatViewFactory.Create(encounter, controlled);

        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        controlled.Clear();
        CombatView second = CombatViewFactory.Create(
            encounter,
            CombatGenericProjectionTestData.CreateControlledCombatantIds());

        AssertEncounterSourceUnchanged(sourceBefore, encounter);
        AssertViewsEquivalent(first, second);
        Assert.Equal(CombatDecisionState.PlayerDecisionRequired,
            first.Decision.State);
        AssertDecisionAgreesWithView(first);
        AssertDecisionAgreesWithView(second);

        Assert.NotSame(first.Combatants, second.Combatants);
        Assert.NotSame(first.Decision.WeaponAttacks,
            second.Decision.WeaponAttacks);
        Assert.NotSame(
            first.Decision.WeaponAttacks[0].Targets,
            second.Decision.WeaponAttacks[0].Targets);
        Assert.NotSame(
            first.Decision.Movement!.DestinationOptions,
            second.Decision.Movement!.DestinationOptions);
        Assert.NotSame(
            first.Decision.Movement.DestinationOptions[0].Path,
            second.Decision.Movement.DestinationOptions[0].Path);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatantView>)first.Combatants)
                .Add(first.Combatants[0]));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatWeaponAttackOption>)first.Decision.WeaponAttacks)
                .Add(first.Decision.WeaponAttacks[0]));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatTargetOption>)first.Decision
                .WeaponAttacks[0].Targets)
                .Add(first.Decision.WeaponAttacks[0].Targets[0]));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<CombatMovementDestinationOption>)first.Decision
                .Movement.DestinationOptions)
                .Add(first.Decision.Movement.DestinationOptions[0]));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<GridPosition>)first.Decision.Movement
                .DestinationOptions[0].Path)
                .Add(new GridPosition(6, 4)));
        AssertEncounterSourceUnchanged(sourceBefore, encounter);

        AssertViewsEquivalent(first, second);
    }

    [Fact]
    public void PublicContract_HasExactTypesAndReadOnlyOutputConstruction()
    {
        Type[] expectedTypes =
        [
            typeof(CombatOperations),
            typeof(CombatView),
            typeof(CombatantView),
            typeof(CombatDecision),
            typeof(CombatDecisionState),
            typeof(CombatMovementOption),
            typeof(CombatMovementDestinationOption),
            typeof(CombatWeaponAttackOption),
            typeof(CombatTargetOption),
            typeof(CombatEndTurnOption)
        ];

        Assembly assembly = typeof(CombatOperations).Assembly;

        Assert.All(expectedTypes, type =>
        {
            Assert.True(type.IsPublic);
            Assert.Contains(type, assembly.GetExportedTypes());
        });

        Type[] outputRecords = expectedTypes
            .Where(type => type != typeof(CombatOperations)
                && type != typeof(CombatDecisionState))
            .ToArray();

        Assert.All(outputRecords, type =>
        {
            Assert.Empty(type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance));
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                property => Assert.False(property.SetMethod?.IsPublic ?? false));
        });

        MethodInfo query = Assert.Single(
            typeof(CombatOperations).GetMethods(
                BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal("Query", query.Name);
        Assert.Equal(typeof(CombatView), query.ReturnType);
        Assert.Equal(
            new[] { typeof(ApplicationSessionState) },
            query.GetParameters().Select(parameter => parameter.ParameterType));

        HashSet<Type> prohibited =
        [
            typeof(EncounterState),
            typeof(EncounterParticipantState),
            typeof(EncounterBattlefieldState),
            typeof(EncounterCombatProfile),
            typeof(CombatTurnState),
            typeof(EncounterMovementResult),
            typeof(EncounterWeaponAttackResult),
            typeof(EncounterDeathSavingThrowResult),
            typeof(EncounterTurnAdvancementResult),
            typeof(AttackResolutionResult),
            typeof(CombatantDamageResult),
            typeof(CombatantState)
        ];

        HashSet<Type> exposed = [];
        foreach (Type type in expectedTypes)
        {
            CollectPublicSignatureTypes(type, exposed);
        }

        Assert.Empty(exposed.Intersect(prohibited));
    }

    private static CombatView CreateGenericPlayerView()
    {
        return CombatViewFactory.Create(
            CombatGenericProjectionTestData.CreatePlayerEncounter(),
            CombatGenericProjectionTestData.CreateControlledCombatantIds());
    }

    private static CombatMovementDestinationOption FindDestination(
        CombatMovementOption movement,
        GridPosition destination)
    {
        return Assert.Single(movement.DestinationOptions.Where(option =>
            option.Destination == destination));
    }

    private static ApplicationSessionState
        CreateControlledWatchtowerEncounterSession()
    {
        ApplicationSessionState session =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = session.ActiveEncounter!.Encounter;
        string controlledCombatantId =
            session.Party.Members[0].PartyMemberId;
        int activePosition = encounter.InitiativeOrder.Single(entry =>
            entry.CombatantId == controlledCombatantId).Position;

        return session with
        {
            ActiveEncounter = session.ActiveEncounter with
            {
                Encounter = encounter with
                {
                    TurnState = encounter.TurnState with
                    {
                        ActivePosition = activePosition
                    },
                    PendingDeathSavingThrowCombatantId = null
                }
            }
        };
    }

    private static void AssertDecisionAgreesWithView(
        CombatView view)
    {
        Assert.Equal(view.Revision, view.Decision.EncounterRevision);
        Assert.Equal(view.ActiveCombatantId,
            view.Decision.ActiveCombatantId);
        Assert.Equal(view.PendingDeathSavingThrowCombatantId,
            view.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Equal(view.WinningSideId,
            view.Decision.WinningSideId);
    }

    private static ApplicationSessionSourceSnapshot CaptureSessionSource(
        ApplicationSessionState session)
    {
        return new ApplicationSessionSourceSnapshot(
            session.ScenarioId,
            session.CurrentMode,
            session.CurrentLocationId,
            session.RandomSeed,
            session.RandomValuesConsumed,
            session.ActiveEncounter,
            session.ActiveEncounter?.Encounter,
            session.ActiveEncounter is null
                ? null
                : CaptureEncounterSource(session.ActiveEncounter.Encounter));
    }

    private static EncounterSourceSnapshot CaptureEncounterSource(
        EncounterState encounter)
    {
        return new EncounterSourceSnapshot(
            encounter,
            encounter.Participants,
            encounter.EncounterId,
            encounter.Revision,
            encounter.LifecycleState,
            encounter.WinningSideId,
            encounter.PendingDeathSavingThrowCombatantId,
            encounter.TurnState.RoundNumber,
            encounter.ActiveCombatantId,
            encounter.Participants
                .Select(participant => new ParticipantSourceSnapshot(
                    participant,
                    participant.Combatant,
                    participant.Combatant.CombatantId,
                    participant.SideId,
                    participant.Position,
                    participant.Combatant.LifecycleState,
                    participant.Combatant.Health,
                    participant.TurnResources.HasActionAvailable,
                    participant.TurnResources.HasBonusActionAvailable,
                    participant.TurnResources.HasReactionAvailable,
                    participant.TurnResources.MovementSpeedFeet,
                    participant.TurnResources.MovementSpentFeet,
                    participant.TurnResources.MovementRemainingFeet,
                    participant.CombatProfile.WeaponAttacks
                        .Select(weapon => new WeaponSourceSnapshot(
                            weapon.WeaponId,
                            weapon.AmmunitionItemId,
                            weapon.AmmunitionQuantityAvailable))
                        .ToArray()))
                .ToArray());
    }

    private static void AssertSessionSourceUnchanged(
        ApplicationSessionSourceSnapshot expected,
        ApplicationSessionState actual)
    {
        Assert.Equal(expected.ScenarioId, actual.ScenarioId);
        Assert.Equal(expected.CurrentMode, actual.CurrentMode);
        Assert.Equal(expected.CurrentLocationId,
            actual.CurrentLocationId);
        Assert.Equal(expected.RandomSeed, actual.RandomSeed);
        Assert.Equal(expected.RandomValuesConsumed,
            actual.RandomValuesConsumed);
        Assert.Same(expected.ActiveEncounterReference,
            actual.ActiveEncounter);
        Assert.Same(expected.EncounterReference,
            actual.ActiveEncounter?.Encounter);

        if (expected.Encounter is null)
        {
            Assert.Null(actual.ActiveEncounter);
            return;
        }

        Assert.NotNull(actual.ActiveEncounter);
        AssertEncounterSourceUnchanged(
            expected.Encounter,
            actual.ActiveEncounter.Encounter);
    }

    private static void AssertEncounterSourceUnchanged(
        EncounterSourceSnapshot expected,
        EncounterState actual)
    {
        Assert.Same(expected.EncounterReference, actual);
        Assert.Same(expected.ParticipantsReference,
            actual.Participants);
        Assert.Equal(expected.EncounterId, actual.EncounterId);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.LifecycleState,
            actual.LifecycleState);
        Assert.Equal(expected.WinningSideId,
            actual.WinningSideId);
        Assert.Equal(expected.PendingDeathSavingThrowCombatantId,
            actual.PendingDeathSavingThrowCombatantId);
        Assert.Equal(expected.RoundNumber,
            actual.TurnState.RoundNumber);
        Assert.Equal(expected.ActiveCombatantId,
            actual.ActiveCombatantId);
        Assert.Equal(expected.Participants.Length,
            actual.Participants.Count);

        for (int index = 0;
            index < expected.Participants.Length;
            index++)
        {
            ParticipantSourceSnapshot expectedParticipant =
                expected.Participants[index];
            EncounterParticipantState actualParticipant =
                actual.Participants[index];

            Assert.Same(expectedParticipant.ParticipantReference,
                actualParticipant);
            Assert.Same(expectedParticipant.CombatantReference,
                actualParticipant.Combatant);
            Assert.Equal(expectedParticipant.CombatantId,
                actualParticipant.Combatant.CombatantId);
            Assert.Equal(expectedParticipant.SideId,
                actualParticipant.SideId);
            Assert.Equal(expectedParticipant.Position,
                actualParticipant.Position);
            Assert.Equal(expectedParticipant.LifecycleState,
                actualParticipant.Combatant.LifecycleState);
            Assert.Same(expectedParticipant.Health,
                actualParticipant.Combatant.Health);
            Assert.Equal(expectedParticipant.Health,
                actualParticipant.Combatant.Health);
            Assert.Equal(expectedParticipant.HasActionAvailable,
                actualParticipant.TurnResources.HasActionAvailable);
            Assert.Equal(expectedParticipant.HasBonusActionAvailable,
                actualParticipant.TurnResources.HasBonusActionAvailable);
            Assert.Equal(expectedParticipant.HasReactionAvailable,
                actualParticipant.TurnResources.HasReactionAvailable);
            Assert.Equal(expectedParticipant.MovementSpeedFeet,
                actualParticipant.TurnResources.MovementSpeedFeet);
            Assert.Equal(expectedParticipant.MovementSpentFeet,
                actualParticipant.TurnResources.MovementSpentFeet);
            Assert.Equal(expectedParticipant.MovementRemainingFeet,
                actualParticipant.TurnResources.MovementRemainingFeet);
            Assert.Equal(expectedParticipant.Weapons.Length,
                actualParticipant.CombatProfile.WeaponAttacks.Count);

            for (int weaponIndex = 0;
                weaponIndex < expectedParticipant.Weapons.Length;
                weaponIndex++)
            {
                WeaponSourceSnapshot expectedWeapon =
                    expectedParticipant.Weapons[weaponIndex];
                FiveEGoldBox.Core.Characters.WeaponAttack actualWeapon =
                    actualParticipant.CombatProfile
                        .WeaponAttacks[weaponIndex];

                Assert.Equal(expectedWeapon.WeaponId,
                    actualWeapon.WeaponId);
                Assert.Equal(expectedWeapon.AmmunitionItemId,
                    actualWeapon.AmmunitionItemId);
                Assert.Equal(expectedWeapon.AmmunitionQuantityAvailable,
                    actualWeapon.AmmunitionQuantityAvailable);
            }
        }
    }

    private static void AssertViewsEquivalent(
        CombatView expected,
        CombatView actual)
    {
        Assert.Equal(expected.EncounterId, actual.EncounterId);
        Assert.Equal(expected.Revision, actual.Revision);
        Assert.Equal(expected.BattlefieldId, actual.BattlefieldId);
        Assert.Equal(expected.BattlefieldWidth,
            actual.BattlefieldWidth);
        Assert.Equal(expected.BattlefieldHeight,
            actual.BattlefieldHeight);
        Assert.Equal(expected.LifecycleState,
            actual.LifecycleState);
        Assert.Equal(expected.RoundNumber, actual.RoundNumber);
        Assert.Equal(expected.ActiveCombatantId,
            actual.ActiveCombatantId);
        Assert.Equal(expected.PendingDeathSavingThrowCombatantId,
            actual.PendingDeathSavingThrowCombatantId);
        Assert.Equal(expected.WinningSideId,
            actual.WinningSideId);
        Assert.Equal(expected.Combatants.Count,
            actual.Combatants.Count);

        for (int index = 0;
            index < expected.Combatants.Count;
            index++)
        {
            CombatantView expectedCombatant =
                expected.Combatants[index];
            CombatantView actualCombatant =
                actual.Combatants[index];

            Assert.Equal(expectedCombatant.CombatantId,
                actualCombatant.CombatantId);
            Assert.Equal(expectedCombatant.SideId,
                actualCombatant.SideId);
            Assert.Equal(expectedCombatant.Position,
                actualCombatant.Position);
            Assert.Equal(expectedCombatant.LifecycleState,
                actualCombatant.LifecycleState);
            Assert.Equal(expectedCombatant.Health,
                actualCombatant.Health);
            Assert.Equal(expectedCombatant.ArmorClass,
                actualCombatant.ArmorClass);
            Assert.Equal(expectedCombatant.MovementSpeedFeet,
                actualCombatant.MovementSpeedFeet);
            Assert.Equal(expectedCombatant.MovementSpentFeet,
                actualCombatant.MovementSpentFeet);
            Assert.Equal(expectedCombatant.MovementRemainingFeet,
                actualCombatant.MovementRemainingFeet);
            Assert.Equal(expectedCombatant.HasActionAvailable,
                actualCombatant.HasActionAvailable);
            Assert.Equal(expectedCombatant.HasBonusActionAvailable,
                actualCombatant.HasBonusActionAvailable);
            Assert.Equal(expectedCombatant.HasReactionAvailable,
                actualCombatant.HasReactionAvailable);
        }

        Assert.Equal(expected.Decision.State,
            actual.Decision.State);
        Assert.Equal(expected.Decision.EncounterRevision,
            actual.Decision.EncounterRevision);
        Assert.Equal(expected.Decision.ActiveCombatantId,
            actual.Decision.ActiveCombatantId);
        Assert.Equal(expected.Decision.PendingDeathSavingThrowCombatantId,
            actual.Decision.PendingDeathSavingThrowCombatantId);
        Assert.Equal(expected.Decision.WinningSideId,
            actual.Decision.WinningSideId);

        AssertMovementOptionsEquivalent(
            expected.Decision.Movement,
            actual.Decision.Movement);
        AssertWeaponOptionsEquivalent(
            expected.Decision.WeaponAttacks,
            actual.Decision.WeaponAttacks);
        AssertEndTurnOptionsEquivalent(
            expected.Decision.EndTurn,
            actual.Decision.EndTurn);
    }

    private static void AssertMovementOptionsEquivalent(
        CombatMovementOption? expected,
        CombatMovementOption? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expected.IsAvailable, actual.IsAvailable);
        Assert.Equal(expected.MovementRemainingFeet,
            actual.MovementRemainingFeet);
        Assert.Equal(expected.UnavailabilityReason,
            actual.UnavailabilityReason);
        Assert.Equal(expected.DestinationOptions.Count,
            actual.DestinationOptions.Count);

        for (int index = 0;
            index < expected.DestinationOptions.Count;
            index++)
        {
            CombatMovementDestinationOption expectedDestination =
                expected.DestinationOptions[index];
            CombatMovementDestinationOption actualDestination =
                actual.DestinationOptions[index];

            Assert.Equal(expectedDestination.Destination,
                actualDestination.Destination);
            Assert.Equal(expectedDestination.MovementCostFeet,
                actualDestination.MovementCostFeet);
            Assert.Equal(expectedDestination.Path,
                actualDestination.Path);
        }
    }

    private static void AssertWeaponOptionsEquivalent(
        IReadOnlyList<CombatWeaponAttackOption> expected,
        IReadOnlyList<CombatWeaponAttackOption> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int weaponIndex = 0;
            weaponIndex < expected.Count;
            weaponIndex++)
        {
            CombatWeaponAttackOption expectedWeapon =
                expected[weaponIndex];
            CombatWeaponAttackOption actualWeapon =
                actual[weaponIndex];

            Assert.Equal(expectedWeapon.WeaponId,
                actualWeapon.WeaponId);
            Assert.Equal(expectedWeapon.IsAvailable,
                actualWeapon.IsAvailable);
            Assert.Equal(expectedWeapon.Targets.Count,
                actualWeapon.Targets.Count);

            for (int targetIndex = 0;
                targetIndex < expectedWeapon.Targets.Count;
                targetIndex++)
            {
                CombatTargetOption expectedTarget =
                    expectedWeapon.Targets[targetIndex];
                CombatTargetOption actualTarget =
                    actualWeapon.Targets[targetIndex];

                Assert.Equal(expectedTarget.TargetCombatantId,
                    actualTarget.TargetCombatantId);
                Assert.Equal(expectedTarget.IsAvailable,
                    actualTarget.IsAvailable);
                Assert.Equal(expectedTarget.UnavailabilityReason,
                    actualTarget.UnavailabilityReason);
                Assert.Equal(expectedTarget.AttackRollMode,
                    actualTarget.AttackRollMode);
                Assert.Equal(expectedTarget.DistanceFeet,
                    actualTarget.DistanceFeet);
            }
        }
    }

    private static void AssertEndTurnOptionsEquivalent(
        CombatEndTurnOption? expected,
        CombatEndTurnOption? actual)
    {
        if (expected is null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expected.IsAvailable, actual.IsAvailable);
        Assert.Equal(expected.UnavailabilityReason,
            actual.UnavailabilityReason);
    }

    private sealed record ApplicationSessionSourceSnapshot(
        string ScenarioId,
        ApplicationMode CurrentMode,
        string CurrentLocationId,
        int RandomSeed,
        int RandomValuesConsumed,
        ActiveEncounterState? ActiveEncounterReference,
        EncounterState? EncounterReference,
        EncounterSourceSnapshot? Encounter);

    private sealed record EncounterSourceSnapshot(
        EncounterState EncounterReference,
        IReadOnlyList<EncounterParticipantState> ParticipantsReference,
        string EncounterId,
        long Revision,
        EncounterLifecycleState LifecycleState,
        string? WinningSideId,
        string? PendingDeathSavingThrowCombatantId,
        int RoundNumber,
        string ActiveCombatantId,
        ParticipantSourceSnapshot[] Participants);

    private sealed record ParticipantSourceSnapshot(
        EncounterParticipantState ParticipantReference,
        CombatantState CombatantReference,
        string CombatantId,
        string SideId,
        GridPosition Position,
        CombatantLifecycleState LifecycleState,
        CombatantHealthState Health,
        bool HasActionAvailable,
        bool HasBonusActionAvailable,
        bool HasReactionAvailable,
        int MovementSpeedFeet,
        int MovementSpentFeet,
        int MovementRemainingFeet,
        WeaponSourceSnapshot[] Weapons);

    private sealed record WeaponSourceSnapshot(
        string WeaponId,
        string? AmmunitionItemId,
        int? AmmunitionQuantityAvailable);

    private static void CollectPublicSignatureTypes(
        Type type,
        ISet<Type> collected)
    {
        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        {
            CollectType(property.PropertyType, collected);
        }

        foreach (MethodInfo method in type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            CollectType(method.ReturnType, collected);
            foreach (ParameterInfo parameter in method.GetParameters())
            {
                CollectType(parameter.ParameterType, collected);
            }
        }
    }

    private static void CollectType(
        Type type,
        ISet<Type> collected)
    {
        if (type.IsArray || type.IsByRef || type.IsPointer)
        {
            CollectType(type.GetElementType()!, collected);
            return;
        }

        if (type.IsGenericType)
        {
            foreach (Type argument in type.GetGenericArguments())
            {
                CollectType(argument, collected);
            }

            return;
        }

        collected.Add(type);
    }
}
