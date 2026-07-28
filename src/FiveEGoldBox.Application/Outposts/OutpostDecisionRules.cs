using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

/// Resolves whatever decision a scenario offers at a hub — any number of
/// options, named however the scenario names them.
///
/// This used to be a fixed accept/decline choice
/// (<c>OutpostMissionChoice</c>), which was really just the shape of the
/// first scenario's own opening decision leaking into the engine. A
/// decision's real shape lives entirely in
/// <see cref="ScenarioDecisionDefinition.Options"/> already; this class just
/// needed to stop assuming there were exactly two of them.
public static class OutpostDecisionRules
{
    private static readonly IReadOnlyList<string> NoAvailableOptions =
        Array.AsReadOnly(Array.Empty<string>());

    public static IReadOnlyList<string> GetAvailableOptionIds(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.CurrentMode != ApplicationMode.Outpost)
        {
            return NoAvailableOptions;
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);
        ScenarioDecisionDefinition? decision =
            FindAvailableDecision(canonicalSession);

        // The scenario authors both which options exist and the order they
        // are offered in.
        return decision is null
            ? NoAvailableOptions
            : Array.AsReadOnly(
                decision.Options
                    .Select(option => option.OptionId)
                    .ToArray());
    }

    public static OutpostDecisionResult Resolve(
        ApplicationSessionState session,
        string optionId)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(optionId))
        {
            throw new ArgumentException(
                "An option ID is required.",
                nameof(optionId));
        }

        ApplicationSessionState canonicalSession =
            ApplicationSessionRules.CreateCanonical(session);
        ScenarioDecisionDefinition? decision =
            FindAvailableDecision(canonicalSession);

        if (decision is null)
        {
            throw new InvalidOperationException(
                "No decision is available here right now.");
        }

        ScenarioDecisionOptionDefinition option = decision.Options
            .FirstOrDefault(candidate => string.Equals(
                candidate.OptionId,
                optionId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Option '{optionId}' is not available on this decision.");

        return Apply(canonicalSession, option);
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

    /// An option that names no resulting progress leaves the scenario where
    /// it stands, which is how declining — or any other non-committal
    /// option — is expressed.
    private static OutpostDecisionResult Apply(
        ApplicationSessionState session,
        ScenarioDecisionOptionDefinition option)
    {
        if (option.ResultingProgressId is null)
        {
            return new OutpostDecisionResult
            {
                OptionId = option.OptionId,
                DidProgressChange = false,
                State = session
            };
        }

        return new OutpostDecisionResult
        {
            OptionId = option.OptionId,
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
}
