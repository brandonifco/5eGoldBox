using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

public static class OutpostMissionRules
{
    public static OutpostMissionResult Resolve(
        ApplicationSessionState session,
        OutpostMissionChoice choice)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!Enum.IsDefined(choice))
        {
            throw new ArgumentOutOfRangeException(
                nameof(choice),
                choice,
                "Unsupported outpost mission choice.");
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonicalSession.Scenario.Progress
            != WatchtowerScenarioProgress.MissionNotAccepted)
        {
            throw new InvalidOperationException(
                "The outpost mission decision is available only before the mission is accepted.");
        }

        return choice switch
        {
            OutpostMissionChoice.AcceptMission =>
                ResolveAcceptance(canonicalSession),
            OutpostMissionChoice.NotYet =>
                new OutpostMissionResult
                {
                    Choice = choice,
                    DidProgressChange = false,
                    State = canonicalSession
                },
            _ => throw new InvalidOperationException(
                "The validated outpost mission choice could not be resolved.")
        };
    }

    private static OutpostMissionResult ResolveAcceptance(
        ApplicationSessionState session)
    {
        ApplicationSessionState acceptedSession =
            ApplicationSessionRules.CreateCanonical(
                session with
                {
                    Scenario = session.Scenario with
                    {
                        Progress =
                            WatchtowerScenarioProgress
                                .MissionAccepted
                    }
                });

        return new OutpostMissionResult
        {
            Choice = OutpostMissionChoice.AcceptMission,
            DidProgressChange = true,
            State = acceptedSession
        };
    }
}
