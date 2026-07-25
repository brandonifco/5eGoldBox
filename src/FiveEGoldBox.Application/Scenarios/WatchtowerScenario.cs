using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios;

/// Translates between the Watchtower scenario's own progress vocabulary and the
/// opaque progress markers the engine stores.
///
/// The identifiers are the enum names verbatim. That keeps the V1 save format
/// and the console's rendered text unchanged from when progress was a field on
/// the session, and it is what a scenario-supplied identifier looks like: a
/// string only this scenario knows how to read.
internal static class WatchtowerScenario
{
    internal static ScenarioState CreateState(
        WatchtowerScenarioProgress progress)
    {
        return new ScenarioState
        {
            ProgressId = ToProgressId(progress)
        };
    }

    internal static string ToProgressId(
        WatchtowerScenarioProgress progress)
    {
        if (!Enum.IsDefined(progress))
        {
            throw new ArgumentOutOfRangeException(
                nameof(progress),
                progress,
                "Unsupported watchtower scenario progress.");
        }

        return progress.ToString();
    }

    /// Reads the progress of a session known to be running Watchtower. Throws
    /// when the marker is not part of this scenario's vocabulary.
    internal static WatchtowerScenarioProgress ProgressOf(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Scenario);

        return ProgressOf(state.Scenario);
    }

    internal static WatchtowerScenarioProgress ProgressOf(
        ScenarioState scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        if (!TryGetProgress(scenario, out WatchtowerScenarioProgress progress))
        {
            throw new ArgumentException(
                $"Progress '{scenario.ProgressId}' is not part of the watchtower scenario.",
                nameof(scenario));
        }

        return progress;
    }

    internal static bool TryGetProgress(
        ScenarioState scenario,
        out WatchtowerScenarioProgress progress)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        return Enum.TryParse(
                scenario.ProgressId,
                ignoreCase: false,
                out progress)
            && Enum.IsDefined(progress);
    }

    internal static ApplicationSessionState WithProgress(
        ApplicationSessionState state,
        WatchtowerScenarioProgress progress)
    {
        ArgumentNullException.ThrowIfNull(state);

        return state with
        {
            Scenario = CreateState(progress)
        };
    }
}
