namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Where an encounter leaves the scenario once it is decided.
internal sealed record EncounterOutcomeDefinition
{
    internal required string VictoryProgressId { get; init; }

    internal required string DefeatProgressId { get; init; }
}
