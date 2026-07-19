using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Travel;

namespace FiveEGoldBox.Application.Sessions;

public sealed record ApplicationSessionState
{
    public required string ScenarioId { get; init; }

    public required ApplicationMode CurrentMode { get; init; }

    public required string CurrentLocationId { get; init; }

    public required PartyState Party { get; init; }

    public required WatchtowerScenarioState Scenario { get; init; }

    public required int RandomSeed { get; init; }

    public required int RandomValuesConsumed { get; init; }

    public RegionalTravelState? RegionalTravel { get; init; }

    public ExplorationState? Exploration { get; init; }

    public ActiveEncounterState? ActiveEncounter { get; init; }
}
