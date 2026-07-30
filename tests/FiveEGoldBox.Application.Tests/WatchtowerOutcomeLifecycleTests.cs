using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerOutcomeLifecycleTests
{
    private const string OutpostLocationId = "location.outpost";

    private const string WatchtowerLocationId =
        "location.ruined-watchtower";

    private const string PartySideId = "side.party";

    private const string FighterId = "party-member.fighter";

    private const string SecondActorId = "party-member.rogue";

    private const string ThirdActorId = "party-member.cleric";

    private const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    private const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    private const string LongswordId = "weapon.longsword";

    private const string MaceId = "weapon.mace";

    private const string ShortbowId = "weapon.shortbow";

    private const string ArrowId = "item.arrow";

    private const int RandomSeed = 8675309;

    [Fact]
    public void WatchtowerScenario_PublicOperations_PartyVictoryProjectsPersistsAndContinuesExploration()
    {
        ApplicationSessionState current =
            ScenarioSessionFactory.CreateNew(WatchtowerScenarioContent.ScenarioId, RandomSeed);
        PartyState startingParty = current.Party;

        Assert.Equal(ApplicationMode.Outpost, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            WatchtowerScenario.ProgressOf(current));
        Assert.Equal(OutpostLocationId, current.CurrentLocationId);
        AssertPartyEquals(startingParty, current.Party);

        ApplicationSessionState beforeMissionDiscovery = current;
        int cursorBeforeMissionDiscovery =
            current.RandomValuesConsumed;
        IReadOnlyList<string> availableChoices =
            OutpostDecisionRules.GetAvailableOptionIds(current);
        string selectedMissionChoice = Assert.Single(
            availableChoices,
            choice => choice == "AcceptMission");

        Assert.Same(beforeMissionDiscovery, current);
        Assert.Equal(
            cursorBeforeMissionDiscovery,
            current.RandomValuesConsumed);
        Assert.Equal(ApplicationMode.Outpost, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionNotAccepted,
            WatchtowerScenario.ProgressOf(current));
        AssertPartyEquals(startingParty, current.Party);

        OutpostDecisionResult missionResult =
            OutpostDecisionRules.Resolve(
                current,
                selectedMissionChoice);
        current = missionResult.State;

        Assert.Equal(
            "AcceptMission",
            missionResult.OptionId);
        Assert.True(missionResult.DidProgressChange);
        Assert.Equal(ApplicationMode.Outpost, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            WatchtowerScenario.ProgressOf(current));

        Assert.True(
            RegionalTravelRules.CanBeginJourney(
                current));

        current = RegionalTravelRules.BeginJourney(
            current);

        Assert.Equal(
            ApplicationMode.RegionalTravel,
            current.CurrentMode);
        Assert.NotNull(current.RegionalTravel);

        int travelAdvances = 0;

        while (!current.RegionalTravel!.IsComplete)
        {
            Assert.True(RegionalTravelRules.CanAdvance(current));

            RegionalTravelAdvanceResult travelResult =
                RegionalTravelRules.Advance(current);
            current = travelResult.State;
            travelAdvances++;

            Assert.Equal(
                current.RegionalTravel!.IsComplete,
                travelResult.DidArrive);
        }

        Assert.True(travelAdvances > 0);
        Assert.False(RegionalTravelRules.CanAdvance(current));
        Assert.Equal(
            WatchtowerLocationId,
            current.CurrentLocationId);
        Assert.True(ExplorationRules.CanEnterDestination(current));

        current = ExplorationRules.EnterDestination(current);

        AssertExploration(
            current,
            "GroundFloor",
            x: 0,
            y: 0,
            ExplorationFacing.East);

        current = MoveForward(
            current,
            "GroundFloor",
            x: 1,
            y: 0,
            ExplorationFacing.East);
        current = MoveForward(
            current,
            "GroundFloor",
            x: 2,
            y: 0,
            ExplorationFacing.East);
        Assert.True(ExplorationRules.CanUseStairs(current));
        current = ExplorationRules.UseStairs(current);

        AssertExploration(
            current,
            "UpperFloor",
            x: 2,
            y: 0,
            ExplorationFacing.East);

        current = Turn(
            current,
            ExplorationTurnDirection.Right,
            "UpperFloor",
            x: 2,
            y: 0,
            ExplorationFacing.South);
        current = MoveForward(
            current,
            "UpperFloor",
            x: 2,
            y: 1,
            ExplorationFacing.South);
        current = Turn(
            current,
            ExplorationTurnDirection.Right,
            "UpperFloor",
            x: 2,
            y: 1,
            ExplorationFacing.West);
        current = MoveForward(
            current,
            "UpperFloor",
            x: 1,
            y: 1,
            ExplorationFacing.West);
        current = Turn(
            current,
            ExplorationTurnDirection.Left,
            "UpperFloor",
            x: 1,
            y: 1,
            ExplorationFacing.South);
        current = Turn(
            current,
            ExplorationTurnDirection.Left,
            "UpperFloor",
            x: 1,
            y: 1,
            ExplorationFacing.East);

        Assert.True(ScenarioTriggerRules.CanActivate(current));

        ExplorationState returnContext = current.Exploration!;
        PartyState preCombatParty = current.Party;
        int cursorBeforeSignalActivation =
            current.RandomValuesConsumed;

        current = ScenarioTriggerRules.Activate(current);

        Assert.Equal(ApplicationMode.Encounter, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            WatchtowerScenario.ProgressOf(current));
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
            CombatDecisionState.CombatCompleted,
            completionDecision.State);
        Assert.Equal(PartySideId, completionDecision.WinningSideId);
        Assert.True(
            current.RandomValuesConsumed > cursorBeforeCombat);

        Assert.Equal(ApplicationMode.Encounter, current.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            WatchtowerScenario.ProgressOf(current));
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

        Assert.Equal(4, authoritativeHealthById.Count);

        EncounterParticipantState archerParticipant =
            Assert.Single(
                completedEncounter.Participants,
                participant => string.Equals(
                    participant.Combatant.CombatantId,
                    SecondActorId,
                    StringComparison.Ordinal));
        var archerBow = Assert.Single(
            archerParticipant.CombatProfile.WeaponAttacks,
            weapon => string.Equals(
                weapon.WeaponId,
                ShortbowId,
                StringComparison.Ordinal));

        Assert.Equal(ArrowId, archerBow.AmmunitionItemId);
        Assert.True(
            archerBow.AmmunitionQuantityAvailable.HasValue);

        int authoritativeArcherAmmunition =
            archerBow.AmmunitionQuantityAvailable.Value;
        int preCombatArcherAmmunition =
            GetPartyMember(preCombatParty, SecondActorId)
                .Ammunition!
                .RemainingQuantity;

        Assert.True(
            authoritativeArcherAmmunition
                < preCombatArcherAmmunition);

        int cursorBeforeFinalization =
            current.RandomValuesConsumed;

        CombatOutcomeResult outcome =
            CombatOutcomeRules.Finalize(current);
        ApplicationSessionState finalized = outcome.State;

        Assert.Equal(
            CombatOutcome.PartyVictory,
            outcome.Outcome);
        Assert.Equal(
            ApplicationMode.Exploration,
            outcome.ResultingMode);
        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.RaidersDefeated),
            outcome.ResultingProgressId);
        Assert.Equal(
            outcome.ResultingMode,
            finalized.CurrentMode);
        Assert.Equal(
            outcome.ResultingProgressId,
            finalized.Scenario.ProgressId);
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

        PartyMemberState finalizedArcher =
            GetPartyMember(finalized.Party, SecondActorId);
        Assert.Equal(
            ShortbowId,
            finalizedArcher.Ammunition!.WeaponId);
        Assert.Equal(
            ArrowId,
            finalizedArcher.Ammunition.AmmunitionItemId);
        Assert.Equal(
            authoritativeArcherAmmunition,
            finalizedArcher.Ammunition.RemainingQuantity);
        Assert.Null(
            GetPartyMember(finalized.Party, FighterId)
                .Ammunition);
        Assert.Null(
            GetPartyMember(finalized.Party, ThirdActorId)
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
            WatchtowerScenario.ProgressOf(loaded));
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
            "UpperFloor",
            x: 1,
            y: 1,
            ExplorationFacing.North);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            WatchtowerScenario.ProgressOf(continued));
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

    /// Fights until the party wins, attacking whatever the engine says is
    /// reachable rather than following a hand-written turn order.
    ///
    /// It used to name every actor, weapon and target in sequence. That
    /// pinned the roster rather than the lifecycle this test is about, and
    /// broke wholesale the moment the party changed — which it now has, to
    /// four characters with different weapons and a different initiative
    /// order. Bounded so a party that cannot win fails rather than hangs.
    ///
    /// Full-strength raiders, no nerf. This used to start them at 3 hit
    /// points on the theory that the baseline party loses the straight damage
    /// race against them — it does not. What actually made the fight
    /// unwinnable was this driver: it only ever attacked or ended a turn, so
    /// a party member with no target in reach simply passed forever while the
    /// raiders (whose own AI does move) closed in and picked characters off.
    /// Chasing the nearest reachable target, below, is the fix.
    private static ApplicationSessionState
        ExecuteBoundedPartyVictoryCombatScript(
            ApplicationSessionState source)
    {
        ApplicationSessionState current = source;

        for (int turn = 0; turn < 60; turn++)
        {
            WatchtowerCombatResolutionResult advanced =
                WatchtowerCombatRules.AdvanceToDecision(current);

            current = advanced.State;

            if (current.ActiveEncounter is null
                || current.ActiveEncounter.Encounter.LifecycleState
                    != EncounterLifecycleState.Active)
            {
                return current;
            }

            WatchtowerCombatDecision decision =
                advanced.ResultingDecision;

            Assert.NotNull(decision.ActiveCombatantId);

            EncounterState encounter =
                current.ActiveEncounter.Encounter;

            // Focus the most hurt reachable raider, with whichever carried
            // weapon can reach it. Spreading damage around loses this fight,
            // which is why the old version was a hand-tuned sequence rather
            // than a loop.
            var attacks = decision.WeaponAttacks
                .Where(weapon => weapon.IsAvailable)
                .SelectMany(weapon => weapon.Targets
                    .Where(candidate => candidate.IsAvailable)
                    .Select(candidate => (weapon.WeaponId, Target: candidate)))
                .OrderBy(candidate => encounter.Participants
                    .Single(participant => string.Equals(
                        participant.Combatant.CombatantId,
                        candidate.Target.TargetCombatantId,
                        StringComparison.Ordinal))
                    .Combatant.Health.HitPoints
                    .CurrentHitPoints)
                .ToArray();

            if (attacks.Length > 0)
            {
                current = ExecuteAttack(
                    current,
                    decision.ActiveCombatantId!,
                    attacks[0].WeaponId,
                    attacks[0].Target.TargetCombatantId);
                continue;
            }

            var unreachable = decision.WeaponAttacks
                .SelectMany(weapon => weapon.Targets
                    .Where(candidate => candidate.UnavailabilityReason
                        == EncounterActionUnavailabilityReason
                            .TargetOutOfRange)
                    .Select(candidate =>
                        (weapon.WeaponId, candidate.TargetCombatantId,
                            candidate.DistanceFeet)))
                .OrderBy(candidate => candidate.DistanceFeet)
                .ThenBy(candidate => candidate.TargetCombatantId)
                .ToArray();
            EncounterMovementResult? movement = unreachable.Length == 0
                ? null
                : WatchtowerCombatPathSearch.FindMovement(
                    encounter,
                    decision.ActiveCombatantId!,
                    unreachable[0].TargetCombatantId,
                    unreachable[0].WeaponId);

            current = movement is null
                ? ExecuteEndTurn(current, decision.ActiveCombatantId!)
                : ExecuteMove(
                    current,
                    decision.ActiveCombatantId!,
                    movement.Path);
        }

        Assert.Fail(
            "The party did not reach victory within the bounded turn count.");

        return current;
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

        WatchtowerCombatWeaponAttackOption weaponOption = Assert.Single(
            decision.WeaponAttacks,
            candidate => candidate.WeaponId == expectedWeaponId);

        Assert.True(weaponOption.IsAvailable);

        WatchtowerCombatTargetOption target = Assert.Single(
            weaponOption.Targets,
            candidate => string.Equals(
                candidate.TargetCombatantId,
                expectedTargetId,
                StringComparison.Ordinal));

        Assert.True(target.IsAvailable);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.Execute(
                advanced.State,
                new CombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    WeaponId = weaponOption.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.NotNull(result.SubmittedIntent);
        Assert.Equal(
            CombatIntentKind.WeaponAttack,
            result.SubmittedIntent!.Kind);
        Assert.Equal(
            decision.ActiveCombatantId,
            result.SubmittedIntent.ActorCombatantId);
        Assert.Equal(
            weaponOption.WeaponId,
            result.SubmittedIntent.WeaponId);
        Assert.Equal(
            target.TargetCombatantId,
            result.SubmittedIntent.TargetCombatantId);
        Assert.Equal(
            CombatStepKind.WeaponAttack,
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
                new CombatEndTurnIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!
                });

        Assert.NotNull(result.SubmittedIntent);
        Assert.Equal(
            CombatIntentKind.EndTurn,
            result.SubmittedIntent!.Kind);
        Assert.Equal(
            decision.ActiveCombatantId,
            result.SubmittedIntent.ActorCombatantId);
        Assert.NotNull(result.PrimaryStep);
        Assert.Equal(
            CombatStepKind.TurnAdvanced,
            result.PrimaryStep!.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            result.PrimaryStep.TurnAdvanceReason);

        return result.State;
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
                new CombatMoveIntent
                {
                    ExpectedEncounterRevision =
                        decision.EncounterRevision,
                    ActorCombatantId = decision.ActiveCombatantId!,
                    Path = path
                });

        return result.State;
    }

    private static void AssertPlayerDecision(
        WatchtowerCombatDecision decision,
        string expectedActorId)
    {
        Assert.Equal(
            CombatDecisionState.PlayerDecisionRequired,
            decision.State);
        Assert.Equal(expectedActorId, decision.ActiveCombatantId);
        Assert.NotNull(decision.Movement);
        Assert.NotEmpty(decision.WeaponAttacks);
        Assert.NotNull(decision.EndTurn);
        Assert.True(decision.EndTurn!.IsAvailable);
    }

    private static ApplicationSessionState MoveForward(
        ApplicationSessionState source,
        string expectedFloor,
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
        string expectedFloor,
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
        string expectedFloor,
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

}
