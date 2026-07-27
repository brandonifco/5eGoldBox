using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Which side is the party's, for whichever scenario is running.
///
/// `CombatOutcomeRules` already reads this off the scenario's own
/// `EncounterDefinition` rather than a Watchtower constant; this is the same
/// lookup, shared so the turn-processing pipeline can stop being the one place
/// that still names Watchtower to answer "whose turn is this."
internal static class EncounterPartySideResolver
{
    internal static string Resolve(
        ApplicationSessionState session,
        EncounterState encounter)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(encounter);

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(session);

        EncounterDefinition encounterDefinition = scenario.Encounters
            .FirstOrDefault(candidate => string.Equals(
                candidate.EncounterId,
                encounter.EncounterId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Encounter '{encounter.EncounterId}' is not part of scenario '{scenario.ScenarioId}'.");

        return encounterDefinition.PartySideId;
    }
}
