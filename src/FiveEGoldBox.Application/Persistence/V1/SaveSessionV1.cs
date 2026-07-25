namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveSessionV1
{
    public required string ScenarioId { get; init; }

    public required SaveApplicationModeV1 CurrentMode { get; init; }

    public required string CurrentLocationId { get; init; }

    public required SavePartyV1 Party { get; init; }

    public required SaveScenarioStateV1 Scenario { get; init; }

    public required int RandomSeed { get; init; }

    public required int RandomValuesConsumed { get; init; }

    public SaveRegionalTravelV1? RegionalTravel { get; init; }

    public SaveExplorationV1? Exploration { get; init; }

    public SaveActiveEncounterV1? ActiveEncounter { get; init; }
}
