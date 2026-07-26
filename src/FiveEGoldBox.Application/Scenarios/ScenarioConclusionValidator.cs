using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios;

/// Checks a session that has reached the end of its scenario.
///
/// Which markers end a scenario, and where the party stands when they do, are
/// declared by the scenario rather than known here - so an adventure with
/// several endings, or one that ends somewhere other than where it was fought,
/// needs no change to this validator.
internal static class ScenarioConclusionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ScenarioConclusionDefinition conclusion =
            ScenarioDefinitionRegistry
                .Resolve(state)
                .Progress
                .Conclusions
                .FirstOrDefault(candidate => string.Equals(
                    candidate.ProgressId,
                    state.Scenario.ProgressId,
                    StringComparison.Ordinal))
            ?? throw new ArgumentException(
                "The scenario conclusion requires progress the scenario declares as an ending.",
                nameof(state));

        if (!string.Equals(
            state.CurrentLocationId,
            conclusion.LocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The scenario conclusion must be at the location its ending declares.",
                nameof(state));
        }

        if (state.RegionalTravel is not null)
        {
            throw new ArgumentException(
                "A scenario conclusion cannot contain regional-travel state.",
                nameof(state));
        }

        if (state.Exploration is not null)
        {
            throw new ArgumentException(
                "A scenario conclusion cannot contain exploration state.",
                nameof(state));
        }

        if (state.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "A scenario conclusion cannot contain an active encounter.",
                nameof(state));
        }
    }
}
