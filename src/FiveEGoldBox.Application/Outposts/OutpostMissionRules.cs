using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

public static class OutpostMissionRules
{
    private static readonly IReadOnlyList<OutpostMissionChoice>
        NoAvailableChoices = Array.AsReadOnly(
            Array.Empty<OutpostMissionChoice>());

    public static IReadOnlyList<OutpostMissionChoice>
        GetAvailableChoices(
            ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return NoAvailableChoices;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);
        ScenarioDecisionDefinition? decision =
            FindAvailableDecision(canonicalSession);

        // The scenario authors both which choices exist and the order they are
        // offered in.
        return decision is null
            ? NoAvailableChoices
            : Array.AsReadOnly(
                decision.Options
                    .Select(ToChoice)
                    .ToArray());
    }

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
        ScenarioDecisionDefinition? decision =
            FindAvailableDecision(canonicalSession);

        if (decision is null)
        {
            throw new InvalidOperationException(
                "The outpost mission decision is available only before the mission is accepted.");
        }

        ScenarioDecisionOptionDefinition? option = decision.Options
            .FirstOrDefault(candidate => ToChoice(candidate) == choice);

        if (option is null)
        {
            throw new InvalidOperationException(
                "The validated outpost mission choice could not be resolved.");
        }

        return Apply(canonicalSession, choice, option);
    }

    /// A decision is on offer when the party is standing where the scenario
    /// puts it and has reached one of the markers it is offered from.
    private static ScenarioDecisionDefinition? FindAvailableDecision(
        ApplicationSessionState session)
    {
        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return null;
        }

        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Decisions
            .FirstOrDefault(decision =>
                string.Equals(
                    decision.LocationId,
                    session.CurrentLocationId,
                    StringComparison.Ordinal)
                && decision.RequiredProgressIds.Contains(
                    session.Scenario.ProgressId,
                    StringComparer.Ordinal));
    }

    /// An option that names no resulting progress leaves the scenario where it
    /// stands, which is how declining is expressed.
    private static OutpostMissionResult Apply(
        ApplicationSessionState session,
        OutpostMissionChoice choice,
        ScenarioDecisionOptionDefinition option)
    {
        if (option.ResultingProgressId is null)
        {
            return new OutpostMissionResult
            {
                Choice = choice,
                DidProgressChange = false,
                State = session
            };
        }

        return new OutpostMissionResult
        {
            Choice = choice,
            DidProgressChange = true,
            State = ApplicationSessionRules.CreateCanonical(
                session with
                {
                    Scenario = new ScenarioState
                    {
                        ProgressId = option.ResultingProgressId
                    }
                })
        };
    }

    private static OutpostMissionChoice ToChoice(
        ScenarioDecisionOptionDefinition option)
    {
        return Enum.TryParse(
                option.OptionId,
                ignoreCase: false,
                out OutpostMissionChoice choice)
            && Enum.IsDefined(choice)
            ? choice
            : throw new InvalidOperationException(
                $"Decision option '{option.OptionId}' is not an outpost mission choice.");
    }
}
