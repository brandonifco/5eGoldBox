using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// Phase 6 step 7: the proof that the engine runs on content rather than on the
/// Watchtower.
///
/// Every rule these tests call is the generic entry point a client uses. None
/// of them is told which scenario is running, and none of the values here comes
/// from Watchtower content — the scenario's own vocabulary is checked against
/// its own constants throughout.
public sealed class SecondScenarioTests
{
    private const int RandomSeed = 4242;

    [Fact]
    public void Definition_IsValidAuthoredContent()
    {
        ValidationResult validation =
            ScenarioDefinitionValidator.Validate(
                ScenarioDefinitionRegistry.Resolve(
                    SunkenChapelScenarioIds.ScenarioId));

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Issues);
    }

    /// A new session begins where the scenario says it begins, on the marker
    /// the scenario declares — not on the first scenario's starting state.
    [Fact]
    public void CreateNew_StartsWhereTheScenarioDeclares()
    {
        ApplicationSessionState session = CreateSession();

        Assert.Equal(
            SunkenChapelScenarioIds.ScenarioId,
            session.ScenarioId);
        Assert.Equal(
            SunkenChapelScenarioIds.HarborLocationId,
            session.CurrentLocationId);
        Assert.Equal(
            SunkenChapelScenarioIds.RumourHeard,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
    }

    /// The whole non-combat path, driven entirely by the definition: take the
    /// commission, walk there, go inside, work the seal, lift the relic.
    [Fact]
    public void Traversal_RunsEndToEndOnGenericEntryPoints()
    {
        ApplicationSessionState session = CreateSession();

        Assert.Equal(
            ["AcceptMission", "NotYet"],
            OutpostDecisionRules.GetAvailableOptionIds(session));

        session = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
            .State;
        Assert.Equal(
            SunkenChapelScenarioIds.CharterSigned,
            session.Scenario.ProgressId);

        Assert.True(RegionalTravelRules.CanBeginJourney(session));
        session = RegionalTravelRules.BeginJourney(session);
        Assert.Equal(
            SunkenChapelScenarioIds.RouteId,
            session.RegionalTravel!.RouteId);

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        Assert.Equal(
            SunkenChapelScenarioIds.ChapelLocationId,
            session.CurrentLocationId);

        Assert.True(ExplorationRules.CanEnterDestination(session));
        session = ExplorationRules.EnterDestination(session);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(0, 0),
            session.Exploration!.Position);

        // Nothing to work until the party is standing on the seal.
        Assert.False(ScenarioTriggerRules.CanActivate(session));
        session = ExplorationRules.MoveForward(session).State;
        Assert.Equal(
            new GridPosition(1, 0),
            session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);

        // A trigger that starts no encounter advances the scenario and leaves
        // the party exploring exactly where it stood.
        Assert.Equal(
            SunkenChapelScenarioIds.SealBroken,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(1, 0),
            session.Exploration!.Position);
        Assert.Null(session.ActiveEncounter);

        session = ExplorationRules.Turn(
            session,
            ExplorationTurnDirection.Right);
        session = ExplorationRules.MoveForward(session).State;
        Assert.Equal(
            new GridPosition(1, 1),
            session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);

        // This scenario's own fight, on its own ground.
        Assert.Equal(ApplicationMode.Encounter, session.CurrentMode);
        Assert.Equal(
            SunkenChapelScenarioIds.GuardiansRoused,
            session.Scenario.ProgressId);

        EncounterState encounter = Assert
            .IsType<ActiveEncounterState>(session.ActiveEncounter)
            .Encounter;
        Assert.Equal(
            SunkenChapelScenarioIds.EncounterId,
            encounter.EncounterId);
        Assert.Equal(
            "battlefield.chapel-nave",
            encounter.Battlefield.BattlefieldId);
        Assert.Equal(
            2,
            encounter.Participants.Count(participant =>
                participant.SideId
                    == SunkenChapelScenarioIds.GuardianSideId));

        session = WinTheFight(session);

        // Winning carries the scenario to the marker its own encounter
        // declares, and puts the party back where it was standing.
        Assert.Equal(
            SunkenChapelScenarioIds.GuardiansBanished,
            session.Scenario.ProgressId);
        Assert.Equal(ApplicationMode.Exploration, session.CurrentMode);
        Assert.Equal(
            new GridPosition(1, 1),
            session.Exploration!.Position);

        Assert.True(ScenarioTriggerRules.CanActivate(session));
        session = ScenarioTriggerRules.Activate(session);

        // A trigger whose resulting marker is a declared conclusion ends the
        // scenario, the same way winning an encounter does.
        Assert.Equal(
            SunkenChapelScenarioIds.RelicRecovered,
            session.Scenario.ProgressId);
        Assert.Equal(
            ApplicationMode.ScenarioConclusion,
            session.CurrentMode);
        Assert.Null(session.Exploration);
        Assert.Null(session.RegionalTravel);
    }

    /// The opposition is built from what the scenario authored, not from the
    /// other adventure's raiders: its own combatants, its own side, and a
    /// dagger whose d4 only became rollable in Phase 7.
    [Fact]
    public void Fight_IsBuiltFromThisScenariosOwnContent()
    {
        ApplicationSessionState session = RunTo(TraversalStage.Fighting);
        EncounterState encounter = Assert
            .IsType<ActiveEncounterState>(session.ActiveEncounter)
            .Encounter;

        EncounterParticipantState guardian = Assert.Single(
            encounter.Participants,
            participant => participant.Combatant.CombatantId
                == "combatant.chapel-guardian.first");

        Assert.Equal(
            SunkenChapelScenarioIds.GuardianSideId,
            guardian.SideId);
        Assert.Equal(7, guardian.Combatant.Health.HitPoints.MaximumHitPoints);
        Assert.Equal(12, guardian.CombatProfile.ArmorClass);

        WeaponAttack dagger = Assert.Single(
            guardian.CombatProfile.WeaponAttacks);
        Assert.Equal(
            SunkenChapelScenarioIds.GuardianDaggerId,
            dagger.WeaponId);
        Assert.Equal(DieType.D4, dagger.Damage.Die);

        // Finesse, so Dexterity: modifier 1, proficiency 2.
        Assert.Equal(Ability.Dexterity, dagger.AttackAbility);
        Assert.Equal(3, dagger.AttackBonus);
        Assert.Equal(1, dagger.DamageBonus);

        // Every party member is deployed alongside them.
        Assert.Equal(
            session.Party.Members.Count + 2,
            encounter.Participants.Count);
    }

    /// Nothing before the fight rolls anything: decisions, travel, movement
    /// and an encounterless trigger are all deterministic.
    [Fact]
    public void Traversal_ConsumesNoRandomnessBeforeTheFight()
    {
        ApplicationSessionState session = RunTo(TraversalStage.SealBroken);

        Assert.Equal(RandomSeed, session.RandomSeed);
        Assert.Equal(0, session.RandomValuesConsumed);
    }

    /// Initiative is one roll per participant, so a scenario with two
    /// opponents consumes what it needs rather than what the first scenario's
    /// five-combatant ambush needed.
    [Fact]
    public void Fight_RollsInitiativeOncePerParticipant()
    {
        ApplicationSessionState session = RunTo(TraversalStage.Fighting);
        EncounterState encounter = Assert
            .IsType<ActiveEncounterState>(session.ActiveEncounter)
            .Encounter;

        Assert.Equal(RandomSeed, session.RandomSeed);
        Assert.Equal(
            encounter.Participants.Count,
            session.RandomValuesConsumed);
        Assert.Equal(
            session.Party.Members.Count + 2,
            session.RandomValuesConsumed);
    }

    /// The save format carries a marker it cannot interpret. Nothing in it
    /// knows this scenario's vocabulary, so a round trip is the evidence that
    /// it does not need to.
    [Theory]
    [InlineData(TraversalStage.Outpost)]
    [InlineData(TraversalStage.Travelling)]
    [InlineData(TraversalStage.Exploring)]
    [InlineData(TraversalStage.SealBroken)]
    [InlineData(TraversalStage.Concluded)]
    public void Save_RoundTripsAtEveryStage(
        TraversalStage stage)
    {
        ApplicationSessionState session = RunTo(stage);
        string saved = ManualSaveSerializer.Serialize(session);

        ManualSaveLoadResult result =
            ManualSaveSerializer.Deserialize(saved);

        Assert.True(result.IsSuccess);
        ApplicationSessionState loaded = Assert.IsType<ApplicationSessionState>(
            result.Session);

        // Re-serializing to the same document is the round-trip evidence:
        // session records hold their party as an IReadOnlyList, so comparing
        // them directly would compare collection references rather than
        // contents.
        Assert.Equal(saved, ManualSaveSerializer.Serialize(loaded));
        Assert.Equal(session.ScenarioId, loaded.ScenarioId);
        Assert.Equal(session.CurrentMode, loaded.CurrentMode);
        Assert.Equal(session.CurrentLocationId, loaded.CurrentLocationId);
        Assert.Equal(
            session.Scenario.ProgressId,
            loaded.Scenario.ProgressId);
    }


    /// Both scenarios resolve their own content from the same registry, and
    /// neither leaks into the other.
    [Fact]
    public void BothScenariosResolveTheirOwnContent()
    {
        ScenarioDefinition chapel = ScenarioDefinitionRegistry.Resolve(
            SunkenChapelScenarioIds.ScenarioId);
        ScenarioDefinition watchtower = ScenarioDefinitionRegistry.Resolve(
            WatchtowerScenarioContent.ScenarioId);

        Assert.NotEqual(watchtower.ScenarioId, chapel.ScenarioId);
        Assert.NotEqual(
            watchtower.StartingLocationId,
            chapel.StartingLocationId);
        Assert.Empty(chapel.Progress.ProgressIds.Intersect(
            watchtower.Progress.ProgressIds,
            StringComparer.Ordinal));
        Assert.Empty(chapel.Locations
            .Select(location => location.LocationId)
            .Intersect(
                watchtower.Locations.Select(location => location.LocationId),
                StringComparer.Ordinal));
    }

    /// A session cannot carry the other scenario's vocabulary, which is what
    /// stops a scenario being validated against content that is not its own.
    [Fact]
    public void ProgressFromTheOtherScenario_IsRejected()
    {
        ApplicationSessionState session = CreateSession();

        Assert.ThrowsAny<ArgumentException>(() =>
            ApplicationSessionRules.Validate(
                session with
                {
                    Scenario = new ScenarioState
                    {
                        ProgressId = WatchtowerScenario.ToProgressId(
                            WatchtowerScenarioProgress.MissionAccepted)
                    }
                }));
    }

    public enum TraversalStage
    {
        Outpost,
        Travelling,
        Exploring,
        SealBroken,
        Fighting,
        Concluded
    }

    private static ApplicationSessionState CreateSession()
    {
        return ScenarioSessionFactory.CreateNew(
            SunkenChapelScenarioIds.ScenarioId,
            RandomSeed);
    }

    internal static ApplicationSessionState RunToConclusion()
    {
        return RunTo(TraversalStage.Concluded);
    }

    internal static ApplicationSessionState RunTo(
        TraversalStage stage)
    {
        ApplicationSessionState session = CreateSession();

        if (stage == TraversalStage.Outpost)
        {
            return session;
        }

        session = OutpostDecisionRules.Resolve(
            session,
            "AcceptMission")
            .State;
        session = RegionalTravelRules.BeginJourney(session);

        if (stage == TraversalStage.Travelling)
        {
            return session;
        }

        while (RegionalTravelRules.CanAdvance(session))
        {
            session = RegionalTravelRules.Advance(session).State;
        }

        session = ExplorationRules.EnterDestination(session);

        if (stage == TraversalStage.Exploring)
        {
            return session;
        }

        session = ExplorationRules.MoveForward(session).State;
        session = ScenarioTriggerRules.Activate(session);

        if (stage == TraversalStage.SealBroken)
        {
            return session;
        }

        session = ExplorationRules.Turn(
            session,
            ExplorationTurnDirection.Right);
        session = ExplorationRules.MoveForward(session).State;
        session = ScenarioTriggerRules.Activate(session);

        if (stage == TraversalStage.Fighting)
        {
            return session;
        }

        session = WinTheFight(session);

        // The relic sits on the square the guardians were roused from, so the
        // party is already standing on it once they are banished.
        return ScenarioTriggerRules.Activate(session);
    }

    /// Ends the encounter with the party victorious without playing it out.
    ///
    /// Enemy tactics are still written per adventure, so nothing yet decides
    /// how a drowned acolyte would act. What this proves is the part that is
    /// generic: an authored encounter resolves through the same outcome rules
    /// the first scenario uses, and its declared victory marker is what the
    /// scenario carries forward.
    private static ApplicationSessionState WinTheFight(
        ApplicationSessionState session)
    {
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(session.ActiveEncounter);
        EncounterState completed = EncounterRules.Complete(
            DefeatTheGuardians(active.Encounter),
            winningSideId: "side.party");

        return CombatOutcomeRules.Finalize(
            session with
            {
                ActiveEncounter = active with
                {
                    Encounter = completed
                }
            })
            .State;
    }

    private static EncounterState DefeatTheGuardians(
        EncounterState encounter)
    {
        EncounterParticipantState[] participants = encounter.Participants
            .Select(participant => participant.SideId
                    == SunkenChapelScenarioIds.GuardianSideId
                ? participant with
                {
                    Combatant = participant.Combatant with
                    {
                        Health = participant.Combatant.Health with
                        {
                            HitPoints = participant.Combatant.Health.HitPoints
                                with
                            {
                                CurrentHitPoints = 0,
                                TemporaryHitPoints = 0
                            }
                        }
                    }
                }
                : participant)
            .ToArray();

        return encounter with
        {
            Participants = Array.AsReadOnly(participants)
        };
    }
}
