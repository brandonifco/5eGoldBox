using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

internal static class WatchtowerCombatTestData
{
    internal static ApplicationSessionState CreatePlayerDecisionSession()
    {
        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(
                WatchtowerSignalTestData.CreateEncounterSession());

        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);

        return result.State;
    }

    internal static ApplicationSessionState CreateDyingActiveSession(
        int randomValuesConsumed,
        int successes,
        int failures)
    {
        ApplicationSessionState source =
            CreatePlayerDecisionSession() with
            {
                RandomValuesConsumed = randomValuesConsumed
            };
        EncounterState encounter = GetEncounter(source);
        string actorId = encounter.ActiveCombatantId;
        EncounterParticipantState actor =
            GetParticipant(source, actorId);

        actor = actor with
        {
            Combatant = actor.Combatant with
            {
                Health = CreateZeroHealth(
                    actor.Combatant.Health.HitPoints.MaximumHitPoints,
                    successes,
                    failures,
                    isStable: false)
            }
        };

        source = ReplaceParticipant(source, actor);
        encounter = GetEncounter(source) with
        {
            PendingDeathSavingThrowCombatantId = actorId
        };

        return ReplaceEncounter(source, encounter);
    }

    internal static ApplicationSessionState AdvanceToCombatant(
        ApplicationSessionState source,
        string combatantId)
    {
        EncounterState current = GetEncounter(source);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            if (string.Equals(
                current.ActiveCombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return ReplaceEncounter(source, current);
            }

            current = EncounterTurnAdvancementRules.Resolve(
                current,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision = current.Revision,
                    ActorCombatantId = current.ActiveCombatantId
                }).State;
        }

        throw new InvalidOperationException(
            $"Combatant '{combatantId}' did not become active.");
    }

    internal static ApplicationSessionState ReplaceParticipant(
        ApplicationSessionState source,
        EncounterParticipantState replacement)
    {
        EncounterState encounter = GetEncounter(source);
        EncounterParticipantState[] participants =
            encounter.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                replacement.Combatant.CombatantId,
                StringComparison.Ordinal));

        Assert.True(index >= 0);
        participants[index] = replacement;

        return ReplaceEncounter(
            source,
            encounter with
            {
                Participants = Array.AsReadOnly(participants)
            });
    }

    internal static ApplicationSessionState ReplaceEncounter(
        ApplicationSessionState source,
        EncounterState encounter)
    {
        ActiveEncounterState active = Assert.IsType<ActiveEncounterState>(
            source.ActiveEncounter);
        ApplicationSessionState state = source with
        {
            ActiveEncounter = active with
            {
                Encounter = encounter
            }
        };

        ApplicationSessionRules.Validate(state);
        return state;
    }

    internal static EncounterState GetEncounter(
        ApplicationSessionState state)
    {
        return Assert.IsType<ActiveEncounterState>(
            state.ActiveEncounter).Encounter;
    }

    internal static EncounterParticipantState GetParticipant(
        ApplicationSessionState state,
        string combatantId)
    {
        return Assert.Single(
            GetEncounter(state).Participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    internal static CombatantHealthState CreateZeroHealth(
        int maximumHitPoints,
        int successes,
        int failures,
        bool isStable)
    {
        return new CombatantHealthState
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = maximumHitPoints,
                CurrentHitPoints = 0,
                TemporaryHitPoints = 0
            },
            DeathSavingThrows = new DeathSavingThrowState
            {
                SuccessCount = successes,
                FailureCount = failures,
                IsStable = isStable
            },
            IsInstantlyDead = false
        };
    }
}
