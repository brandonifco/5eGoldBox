using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Exploration;

public static class SignalMechanismRules
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

        if (session.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            return false;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        return WatchtowerSignalMechanism.CanActivate(
            canonicalSession.Exploration!);
    }

    public static ApplicationSessionState Activate(
        ApplicationSessionState session,
        ValidatedRuleset ruleset)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(ruleset);

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.CurrentMode
            != ApplicationMode.Exploration)
        {
            throw new InvalidOperationException(
                "The signal mechanism can be activated only in exploration mode.");
        }

        ExplorationState returnContext =
            canonicalSession.Exploration!;

        if (!WatchtowerSignalMechanism.CanActivate(
            returnContext))
        {
            throw new InvalidOperationException(
                "The party is not in the authored position and facing required to activate the signal mechanism.");
        }

        if (canonicalSession.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            throw new InvalidOperationException(
                "The signal mechanism can be activated only while the accepted mission is active.");
        }

        EncounterState encounter =
            WatchtowerSignalEncounter.Create(
                canonicalSession.Party,
                ruleset,
                canonicalSession.RandomSeed,
                canonicalSession.RandomValuesConsumed,
                out int updatedRandomValuesConsumed);

        return ApplicationSessionRules.CreateCanonical(
            canonicalSession with
            {
                CurrentMode = ApplicationMode.Encounter,
                Scenario = canonicalSession.Scenario with
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .SignalActivated
                },
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
}
