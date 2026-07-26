using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public static class ScenarioTriggerRules
{
    public static bool CanActivate(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode
            != ApplicationMode.Exploration)
        {
            return false;
        }

        if (session.Scenario is null
            || session.Exploration is null)
        {
            ApplicationSessionRules.CreateCanonical(
                session);

            throw new InvalidOperationException(
                "The malformed exploration session could not be canonicalized.");
        }

        // Checked before canonicalising: a session whose progress no trigger
        // wants may be one this mode would reject outright, and the caller
        // expects false rather than an exception.
        if (!HasProgressForAnyTrigger(session))
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        // Whether there is anything to interact with here is the scenario's
        // to say, not this rule's.
        return ScenarioTriggerMatcher.FindAvailable(canonicalSession)
            is not null;
    }

    public static ApplicationSessionState Activate(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Resolved lazily: a trigger that starts no encounter needs no
        // ruleset, and should not have to reach for one scenario's content to
        // get it.
        return ActivateCanonical(
            session,
            WatchtowerScenarioContent.CreateRuleset);
    }

    public static ApplicationSessionState Activate(
        ApplicationSessionState session,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(ruleset);

        return ActivateCanonical(session, () => ruleset);
    }

    private static ApplicationSessionState ActivateCanonical(
        ApplicationSessionState session,
        Func<ValidatedRuleset> resolveRuleset)
    {
        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.Exploration)
        {
            throw new InvalidOperationException(
                "A trigger can be activated only in exploration mode.");
        }

        ScenarioTriggerDefinition? trigger =
            ScenarioTriggerMatcher.FindAvailable(canonicalSession);

        if (trigger is null)
        {
            // Distinguish standing in the wrong place from having the wrong
            // progress, so the caller keeps the message it had before.
            throw new InvalidOperationException(
                HasProgressForAnyTrigger(canonicalSession)
                    ? "The party is not in the authored position and facing required to activate this trigger."
                    : "No trigger here is available at the scenario's current progress.");
        }

        ScenarioState resultingScenario = new()
        {
            ProgressId = trigger.ResultingProgressId
        };

        // Starting an encounter is one thing a trigger can do, not the only
        // thing. A trigger that names no encounter - a lever, an inscription -
        // moves the scenario on and leaves the party where it was standing,
        // unless what it moves the scenario to is an ending.
        if (trigger.EncounterId is null)
        {
            bool concludesScenario = ScenarioDefinitionRegistry
                .Resolve(canonicalSession)
                .Progress
                .Conclusions
                .Any(conclusion => string.Equals(
                    conclusion.ProgressId,
                    trigger.ResultingProgressId,
                    StringComparison.Ordinal));

            return ApplicationSessionRules.CreateCanonical(
                canonicalSession with
                {
                    CurrentMode = concludesScenario
                        ? ApplicationMode.ScenarioConclusion
                        : canonicalSession.CurrentMode,
                    Scenario = resultingScenario,
                    Exploration = concludesScenario
                        ? null
                        : canonicalSession.Exploration
                });
        }

        EncounterDefinition encounterDefinition =
            ScenarioDefinitionRegistry
                .Resolve(canonicalSession)
                .Encounters
                .FirstOrDefault(candidate => string.Equals(
                    candidate.EncounterId,
                    trigger.EncounterId,
                    StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Trigger '{trigger.TriggerId}' starts encounter '{trigger.EncounterId}', which this scenario does not declare.");

        ExplorationState returnContext =
            canonicalSession.Exploration!;
        EncounterState encounter =
            ScenarioEncounterFactory.Create(
                encounterDefinition,
                canonicalSession.Party,
                resolveRuleset(),
                canonicalSession.RandomSeed,
                canonicalSession.RandomValuesConsumed,
                out int updatedRandomValuesConsumed);

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode = ApplicationMode.Encounter,
                Scenario = resultingScenario,
                Exploration = null,
                ActiveEncounter = new ActiveEncounterState
                {
                    ReturnContext = returnContext,
                    Encounter = encounter
                },
                RandomValuesConsumed =
                    updatedRandomValuesConsumed
            });
    }

    private static bool HasProgressForAnyTrigger(
        ApplicationSessionState session)
    {
        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Triggers
            .Any(trigger => trigger.RequiredProgressIds.Contains(
                session.Scenario.ProgressId,
                StringComparer.Ordinal));
    }
}
