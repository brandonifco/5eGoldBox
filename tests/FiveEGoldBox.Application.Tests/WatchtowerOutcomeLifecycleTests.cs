using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerOutcomeLifecycleTests
{
    private const string ScenarioId = "scenario.watchtower";

    private const string OutpostLocationId = "location.outpost";

    private const string WatchtowerLocationId =
        "location.ruined-watchtower";

    private const string PartySideId = "side.party";

    private const string FighterId = "party-member.fighter";

    private const string BarbarianId = "party-member.barbarian";

    private const string RangerId = "party-member.ranger";

    private const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    private const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    private const string LongswordId = "weapon.longsword";

    private const string GreataxeId = "weapon.greataxe";

    private const string LongbowId = "weapon.longbow";

    private const string ArrowId = "item.arrow";

    private const int RandomSeed = 8675309;

    [Fact]
    public void WatchtowerScenario_PublicOperations_PartyVictoryProjectsPersistsAndContinuesExploration()
    {
        PartyState startingParty = CreateCanonicalParty();
        ValidatedRuleset ruleset =
            WatchtowerSignalTestData.CreateRuleset();

        ApplicationSessionState current =
            ApplicationSessionRules.CreateNew(
                ScenarioId,
                OutpostLocationId,
                startingParty,
                RandomSeed);

        Assert.Equal(ApplicationMode.Outpost, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            current.Scenario.Progress);
        Assert.Equal(OutpostLocationId, current.CurrentLocationId);
        AssertPartyEquals(startingParty, current.Party);

        OutpostMissionResult missionResult =
            OutpostMissionRules.Resolve(
                current,
                OutpostMissionChoice.AcceptMission);
        current = missionResult.State;

        Assert.Equal(
            OutpostMissionChoice.AcceptMission,
            missionResult.Choice);
        Assert.True(missionResult.DidProgressChange);
        Assert.Equal(ApplicationMode.Outpost, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            current.Scenario.Progress);

        current = RegionalTravelRules.BeginWatchtowerJourney(
            current);

        Assert.Equal(
            ApplicationMode.RegionalTravel,
            current.CurrentMode);
        Assert.NotNull(current.RegionalTravel);

        int travelAdvances = 0;

        while (!current.RegionalTravel!.IsComplete)
        {
            RegionalTravelAdvanceResult travelResult =
                RegionalTravelRules.Advance(current);
            current = travelResult.State;
            travelAdvances++;

            Assert.Equal(
                current.RegionalTravel!.IsComplete,
                travelResult.DidArrive);
        }

        Assert.True(travelAdvances > 0);
        Assert.Equal(
            WatchtowerLocationId,
            current.CurrentLocationId);

        current = ExplorationRules.EnterWatchtower(current);

        AssertExploration(
            current,
            ExplorationFloor.GroundFloor,
            x: 0,
            y: 0,
            ExplorationFacing.East);

        current = MoveForward(
            current,
            ExplorationFloor.GroundFloor,
            x: 1,
            y: 0,
            ExplorationFacing.East);
        current = MoveForward(
            current,
            ExplorationFloor.GroundFloor,
            x: 2,
            y: 0,
            ExplorationFacing.East);
        current = ExplorationRules.UseStairs(current);

        AssertExploration(
            current,
            ExplorationFloor.UpperFloor,
            x: 2,
            y: 0,
            ExplorationFacing.East);

        current = Turn(
            current,
            ExplorationTurnDirection.Right,
            ExplorationFloor.UpperFloor,
            x: 2,
            y: 0,
            ExplorationFacing.South);
        current = MoveForward(
            current,
            ExplorationFloor.UpperFloor,
            x: 2,
            y: 1,
            ExplorationFacing.South);
        current = Turn(
            current,
            ExplorationTurnDirection.Right,
            ExplorationFloor.UpperFloor,
            x: 2,
            y: 1,
            ExplorationFacing.West);
        current = MoveForward(
            current,
            ExplorationFloor.UpperFloor,
            x: 1,
            y: 1,
            ExplorationFacing.West);
        current = Turn(
            current,
            ExplorationTurnDirection.Left,
            ExplorationFloor.UpperFloor,
            x: 1,
            y: 1,
            ExplorationFacing.South);
        current = Turn(
            current,
            ExplorationTurnDirection.Left,
            ExplorationFloor.UpperFloor,
            x: 1,
            y: 1,
            ExplorationFacing.East);

        Assert.True(SignalMechanismRules.CanActivate(current));

        ExplorationState returnContext = current.Exploration!;
        PartyState preCombatParty = current.Party;
        int cursorBeforeSignalActivation =
            current.RandomValuesConsumed;

        current = SignalMechanismRules.Activate(
            current,
            ruleset);

        Assert.Equal(ApplicationMode.Encounter, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            current.Scenario.Progress);
        Assert.Null(current.Exploration);
        Assert.Null(current.RegionalTravel);
        Assert.NotNull(current.ActiveEncounter);
        Assert.Equal(
            returnContext,
            current.ActiveEncounter!.ReturnContext);
        Assert.True(
            current.RandomValuesConsumed
                > cursorBeforeSignalActivation);
        AssertPartyEquals(preCombatParty, current.Party);

        int cursorBeforeCombat = current.RandomValuesConsumed;

        current = ExecuteBoundedPartyVictoryCombatScript(current);

        WatchtowerCombatResolutionResult completionResult =
            WatchtowerCombatRules.AdvanceToDecision(current);
        current = completionResult.State;
        WatchtowerCombatDecision completionDecision =
            completionResult.ResultingDecision;

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            completionDecision.State);
        Assert.Equal(PartySideId, completionDecision.WinningSideId);
        Assert.True(
            current.RandomValuesConsumed > cursorBeforeCombat);

        Assert.Equal(ApplicationMode.Encounter, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            current.Scenario.Progress);
        Assert.Null(current.Exploration);
        Assert.NotNull(current.ActiveEncounter);
        Assert.Equal(
            returnContext,
            current.ActiveEncounter!.ReturnContext);
        AssertPartyEquals(preCombatParty, current.Party);

        EncounterState completedEncounter =
            current.ActiveEncounter.Encounter;

        Assert.Equal(
            EncounterLifecycleState.Completed,
            completedEncounter.LifecycleState);
        Assert.Equal(PartySideId, completedEncounter.WinningSideId);
        Assert.DoesNotContain(
            completedEncounter.Participants,
            participant => participant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying);

        IReadOnlyDictionary<string, CombatantHealthState>
            authoritativeHealthById =
                completedEncounter.Participants
                    .Where(participant => string.Equals(
                        participant.SideId,
                        PartySideId,
                        StringComparison.Ordinal))
                    .ToDictionary(
                        participant =>
                            participant.Combatant.CombatantId,
                        participant =>
                            participant.Combatant.Health,
                        StringComparer.Ordinal);

        Assert.Equal(3, authoritativeHealthById.Count);

        EncounterParticipantState rangerParticipant =
            Assert.Single(
                completedEncounter.Participants,
                participant => string.Equals(
                    participant.Combatant.CombatantId,
                    RangerId,
                    StringComparison.Ordinal));
        var rangerLongbow = Assert.Single(
            rangerParticipant.CombatProfile.WeaponAttacks,
            weapon => string.Equals(
                weapon.WeaponId,
                LongbowId,
                StringComparison.Ordinal));

        Assert.Equal(ArrowId, rangerLongbow.AmmunitionItemId);
        Assert.True(
            rangerLongbow.AmmunitionQuantityAvailable.HasValue);

        int authoritativeRangerAmmunition =
            rangerLongbow.AmmunitionQuantityAvailable.Value;
        int preCombatRangerAmmunition =
            GetPartyMember(preCombatParty, RangerId)
                .Ammunition!
                .RemainingQuantity;

        Assert.True(
            authoritativeRangerAmmunition
                < preCombatRangerAmmunition);

        int cursorBeforeFinalization =
            current.RandomValuesConsumed;

        WatchtowerCombatOutcomeResult outcome =
            WatchtowerCombatOutcomeRules.Finalize(current);
        ApplicationSessionState finalized = outcome.State;

        Assert.Equal(
            WatchtowerCombatOutcome.PartyVictory,
            outcome.Outcome);
        Assert.Equal(
            ApplicationMode.Exploration,
            outcome.ResultingMode);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            outcome.ResultingProgress);
        Assert.Equal(
            outcome.ResultingMode,
            finalized.CurrentMode);
        Assert.Equal(
            outcome.ResultingProgress,
            finalized.Scenario.Progress);
        Assert.Equal(
            WatchtowerLocationId,
            finalized.CurrentLocationId);
        Assert.Equal(returnContext, finalized.Exploration);
        Assert.Null(finalized.RegionalTravel);
        Assert.Null(finalized.ActiveEncounter);
        Assert.Equal(RandomSeed, finalized.RandomSeed);
        Assert.Equal(
            cursorBeforeFinalization,
            finalized.RandomValuesConsumed);

        AssertUnrelatedPartyStatePreserved(
            preCombatParty,
            finalized.Party);
        AssertPartyHealthMatchesAuthority(
            finalized.Party,
            authoritativeHealthById);

        PartyMemberState finalizedRanger =
            GetPartyMember(finalized.Party, RangerId);
        Assert.Equal(
            LongbowId,
            finalizedRanger.Ammunition!.WeaponId);
        Assert.Equal(
            ArrowId,
            finalizedRanger.Ammunition.AmmunitionItemId);
        Assert.Equal(
            authoritativeRangerAmmunition,
            finalizedRanger.Ammunition.RemainingQuantity);
        Assert.Null(
            GetPartyMember(finalized.Party, FighterId)
                .Ammunition);
        Assert.Null(
            GetPartyMember(finalized.Party, BarbarianId)
                .Ammunition);

        string serialized =
            ManualSaveSerializer.Serialize(finalized);
        ManualSaveLoadResult loadResult =
            ManualSaveSerializer.Deserialize(serialized);

        Assert.True(loadResult.IsSuccess);
        Assert.Null(loadResult.FailureReason);

        ApplicationSessionState loaded = Assert.IsType<
            ApplicationSessionState>(loadResult.Session);

        Assert.Equal(
            ApplicationMode.Exploration,
            loaded.CurrentMode);
        Assert.Equal(
            WatchtowerLocationId,
            loaded.CurrentLocationId);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            loaded.Scenario.Progress);
        Assert.Equal(returnContext, loaded.Exploration);
        Assert.Null(loaded.RegionalTravel);
        Assert.Null(loaded.ActiveEncounter);
        Assert.Equal(finalized.RandomSeed, loaded.RandomSeed);
        Assert.Equal(
            finalized.RandomValuesConsumed,
            loaded.RandomValuesConsumed);
        AssertPartyEquals(finalized.Party, loaded.Party);

        ApplicationSessionState continued =
            ExplorationRules.Turn(
                loaded,
                ExplorationTurnDirection.Left);

        AssertExploration(
            continued,
            ExplorationFloor.UpperFloor,
            x: 1,
            y: 1,
            ExplorationFacing.North);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            continued.Scenario.Progress);
        Assert.Equal(
            WatchtowerLocationId,
            continued.CurrentLocationId);
        Assert.Equal(loaded.RandomSeed, continued.RandomSeed);
        Assert.Equal(
            loaded.RandomValuesConsumed,
            continued.RandomValuesConsumed);
        Assert.Null(continued.RegionalTravel);
        Assert.Null(continued.ActiveEncounter);
        AssertPartyEquals(loaded.Party, continued.Party);
    }

    private static ApplicationSessionState
        ExecuteBoundedPartyVictoryCombatScript(
            ApplicationSessionState source)
    {
        ApplicationSessionState current = source;

        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteAttack(
            current,
            FighterId,
            LongswordId,
            MeleeRaiderId);
        current = ExecuteEndTurn(current, FighterId);
        current = ExecuteAttack(
            current,
            BarbarianId,
            GreataxeId,
            MeleeRaiderId);
        current = ExecuteEndTurn(current, BarbarianId);
        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            MeleeRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteAttack(
            current,
            BarbarianId,
            GreataxeId,
            MeleeRaiderId);
        current = ExecuteEndTurn(current, BarbarianId);
        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            MeleeRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteMove(
            current,
            FighterId,
            [
                new GridPosition(2, 0),
                new GridPosition(3, 1)
            ]);
        current = ExecuteAttack(
            current,
            FighterId,
            LongswordId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, FighterId);
        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteAttack(
            current,
            FighterId,
            LongswordId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, FighterId);
        current = ExecuteAttack(
            current,
            RangerId,
            LongbowId,
            RangedRaiderId);
        current = ExecuteEndTurn(current, RangerId);
        current = ExecuteAttack(
            current,
            FighterId,
            LongswordId,
            RangedRaiderId);

        return current;
    }

    private static ApplicationSessionState ExecuteMove(
        ApplicationSessionState source,
        string expectedActorId,
        IReadOnlyList<GridPosition> path)
    {
        WatchtowerCombatResolutionResult advanced =
            WatchtowerCombatRules.AdvanceToDecision(source);
        WatchtowerCombatDecision decision =
            advanced.ResultingDecision;

        AssertPlayerDecision(decision, expectedActorId);
        Assert.True(decision.Movement!.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                advanced.State,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = expectedActorId,
                    Path = path
                });

        Assert.NotNull(result.SubmittedIntent);
        Assert.Equal(
            WatchtowerCombatIntentKind.Move,
            result.SubmittedIntent!.Kind);
        Assert.Equal(
            expectedActorId,
            result.SubmittedIntent.ActorCombatantId);
        Assert.Equal(
            path.ToArray(),
            result.SubmittedIntent.Path.ToArray());
        Assert.Equal(
            WatchtowerCombatStepKind.Movement,
            result.PrimaryStep!.Kind);
        Assert.Equal(
            expectedActorId,
            result.PrimaryStep.ActorCombatantId);

        return result.State;
    }

    private static ApplicationSessionState ExecuteAttack(
        ApplicationSessionState source,
        string expectedActorId,
        string expectedWeaponId,
        string expectedTargetId)
    {
        WatchtowerCombatResolutionResult advanced =
            WatchtowerCombatRules.AdvanceToDecision(source);
        WatchtowerCombatDecision decision =
            advanced.ResultingDecision;

        AssertPlayerDecision(decision, expectedActorId);
        Assert.True(decision.WeaponAttack!.IsAvailable);
        Assert.Equal(
            expectedWeaponId,
            decision.WeaponAttack.WeaponId);

        WatchtowerCombatTargetOption target = Assert.Single(
            decision.WeaponAttack.Targets,
            candidate => string.Equals(
                candidate.TargetCombatantId,
                expectedTargetId,
                StringComparison.Ordinal));

        Assert.True(target.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                advanced.State,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = expectedActorId,
                    WeaponId = expectedWeaponId,
                    TargetCombatantId = expectedTargetId
                });

        Assert.NotNull(result.SubmittedIntent);
        Assert.Equal(
            WatchtowerCombatIntentKind.WeaponAttack,
            result.SubmittedIntent!.Kind);
        Assert.Equal(
            expectedActorId,
            result.SubmittedIntent.ActorCombatantId);
        Assert.Equal(
            expectedWeaponId,
            result.SubmittedIntent.WeaponId);
        Assert.Equal(
            expectedTargetId,
            result.SubmittedIntent.TargetCombatantId);
        Assert.Equal(
            WatchtowerCombatStepKind.WeaponAttack,
            result.PrimaryStep!.Kind);
        Assert.Equal(
            expectedActorId,
            result.PrimaryStep.ActorCombatantId);
        Assert.Equal(
            expectedTargetId,
            result.PrimaryStep.TargetCombatantId);

        return result.State;
    }

    private static ApplicationSessionState ExecuteEndTurn(
        ApplicationSessionState source,
        string expectedActorId)
    {
        WatchtowerCombatResolutionResult advanced =
            WatchtowerCombatRules.AdvanceToDecision(source);
        WatchtowerCombatDecision decision =
            advanced.ResultingDecision;

        AssertPlayerDecision(decision, expectedActorId);
        Assert.True(decision.EndTurn!.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                advanced.State,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = expectedActorId
                });

        Assert.NotNull(result.SubmittedIntent);
        Assert.Equal(
            WatchtowerCombatIntentKind.EndTurn,
            result.SubmittedIntent!.Kind);
        Assert.Equal(
            expectedActorId,
            result.SubmittedIntent.ActorCombatantId);
        Assert.NotNull(result.PrimaryStep);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            result.PrimaryStep!.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            result.PrimaryStep.TurnAdvanceReason);

        return result.State;
    }

    private static void AssertPlayerDecision(
        WatchtowerCombatDecision decision,
        string expectedActorId)
    {
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            decision.State);
        Assert.Equal(expectedActorId, decision.ActiveCombatantId);
        Assert.NotNull(decision.Movement);
        Assert.NotNull(decision.WeaponAttack);
        Assert.NotNull(decision.EndTurn);
        Assert.True(decision.EndTurn!.IsAvailable);
    }

    private static ApplicationSessionState MoveForward(
        ApplicationSessionState source,
        ExplorationFloor expectedFloor,
        int x,
        int y,
        ExplorationFacing expectedFacing)
    {
        ExplorationMoveResult result =
            ExplorationRules.MoveForward(source);

        Assert.True(result.DidMove);
        AssertExploration(
            result.State,
            expectedFloor,
            x,
            y,
            expectedFacing);

        return result.State;
    }

    private static ApplicationSessionState Turn(
        ApplicationSessionState source,
        ExplorationTurnDirection direction,
        ExplorationFloor expectedFloor,
        int x,
        int y,
        ExplorationFacing expectedFacing)
    {
        ApplicationSessionState result =
            ExplorationRules.Turn(source, direction);

        AssertExploration(
            result,
            expectedFloor,
            x,
            y,
            expectedFacing);

        return result;
    }

    private static void AssertExploration(
        ApplicationSessionState state,
        ExplorationFloor expectedFloor,
        int x,
        int y,
        ExplorationFacing expectedFacing)
    {
        Assert.Equal(ApplicationMode.Exploration, state.CurrentMode);
        Assert.Equal(
            WatchtowerLocationId,
            state.CurrentLocationId);
        Assert.NotNull(state.Exploration);
        Assert.Equal(
            "map.ruined-watchtower",
            state.Exploration!.MapId);
        Assert.Equal(expectedFloor, state.Exploration.Floor);
        Assert.Equal(
            new GridPosition(x, y),
            state.Exploration.Position);
        Assert.Equal(expectedFacing, state.Exploration.Facing);
    }

    private static void AssertPartyHealthMatchesAuthority(
        PartyState actual,
        IReadOnlyDictionary<string, CombatantHealthState>
            authoritativeHealthById)
    {
        foreach (PartyMemberState member in actual.Members)
        {
            Assert.True(
                authoritativeHealthById.TryGetValue(
                    member.PartyMemberId,
                    out CombatantHealthState? expected));
            AssertHealthEquals(expected!, member.Health);
        }
    }

    private static void AssertUnrelatedPartyStatePreserved(
        PartyState expected,
        PartyState actual)
    {
        Assert.Equal(expected.PartyId, actual.PartyId);
        Assert.Equal(expected.Members.Count, actual.Members.Count);

        for (int index = 0; index < expected.Members.Count; index++)
        {
            PartyMemberState expectedMember =
                expected.Members[index];
            PartyMemberState actualMember =
                actual.Members[index];

            Assert.Equal(
                expectedMember.PartyMemberId,
                actualMember.PartyMemberId);
            Assert.Equal(
                expectedMember.CharacterDefinitionId,
                actualMember.CharacterDefinitionId);
            Assert.Equal(
                expectedMember.DisplayName,
                actualMember.DisplayName);
            Assert.Equal(
                expectedMember.ClassId,
                actualMember.ClassId);
            Assert.Equal(
                expectedMember.ZeroHitPointPolicy,
                actualMember.ZeroHitPointPolicy);
            Assert.Equal(
                expectedMember.Ammunition?.WeaponId,
                actualMember.Ammunition?.WeaponId);
            Assert.Equal(
                expectedMember.Ammunition?.AmmunitionItemId,
                actualMember.Ammunition?.AmmunitionItemId);
        }
    }

    private static void AssertPartyEquals(
        PartyState expected,
        PartyState actual)
    {
        AssertUnrelatedPartyStatePreserved(expected, actual);

        for (int index = 0; index < expected.Members.Count; index++)
        {
            PartyMemberState expectedMember =
                expected.Members[index];
            PartyMemberState actualMember =
                actual.Members[index];

            AssertHealthEquals(
                expectedMember.Health,
                actualMember.Health);
            Assert.Equal(
                expectedMember.Ammunition?.RemainingQuantity,
                actualMember.Ammunition?.RemainingQuantity);
        }
    }

    private static void AssertHealthEquals(
        CombatantHealthState expected,
        CombatantHealthState actual)
    {
        Assert.Equal(
            expected.HitPoints.MaximumHitPoints,
            actual.HitPoints.MaximumHitPoints);
        Assert.Equal(
            expected.HitPoints.CurrentHitPoints,
            actual.HitPoints.CurrentHitPoints);
        Assert.Equal(
            expected.HitPoints.TemporaryHitPoints,
            actual.HitPoints.TemporaryHitPoints);
        Assert.Equal(
            expected.DeathSavingThrows.SuccessCount,
            actual.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            expected.DeathSavingThrows.FailureCount,
            actual.DeathSavingThrows.FailureCount);
        Assert.Equal(
            expected.DeathSavingThrows.IsStable,
            actual.DeathSavingThrows.IsStable);
        Assert.Equal(
            expected.IsInstantlyDead,
            actual.IsInstantlyDead);
        Assert.Equal(expected.IsDead, actual.IsDead);
    }

    private static PartyMemberState GetPartyMember(
        PartyState party,
        string partyMemberId)
    {
        return Assert.Single(
            party.Members,
            member => string.Equals(
                member.PartyMemberId,
                partyMemberId,
                StringComparison.Ordinal));
    }

    private static PartyState CreateCanonicalParty()
    {
        return new PartyState
        {
            PartyId = "party.player",
            Members =
            [
                CreatePartyMember(
                    FighterId,
                    "character.fighter",
                    "Fighter",
                    "class.fighter",
                    maximumHitPoints: 12,
                    currentHitPoints: 8,
                    temporaryHitPoints: 2,
                    ammunition: null),
                CreatePartyMember(
                    BarbarianId,
                    "character.barbarian",
                    "Barbarian",
                    "class.barbarian",
                    maximumHitPoints: 14,
                    currentHitPoints: 14,
                    temporaryHitPoints: 0,
                    ammunition: null),
                CreatePartyMember(
                    RangerId,
                    "character.ranger",
                    "Ranger",
                    "class.ranger",
                    maximumHitPoints: 11,
                    currentHitPoints: 11,
                    temporaryHitPoints: 0,
                    ammunition: new AmmunitionState
                    {
                        WeaponId = LongbowId,
                        AmmunitionItemId = ArrowId,
                        RemainingQuantity = 7
                    })
            ]
        };
    }

    private static PartyMemberState CreatePartyMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints,
        AmmunitionState? ammunition)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId = characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = maximumHitPoints,
                    CurrentHitPoints = currentHitPoints,
                    TemporaryHitPoints = temporaryHitPoints
                },
                DeathSavingThrows = new DeathSavingThrowState
                {
                    SuccessCount = 0,
                    FailureCount = 0,
                    IsStable = false
                },
                IsInstantlyDead = false
            },
            Ammunition = ammunition
        };
    }
}
