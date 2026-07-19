using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

public sealed record ActiveEncounterState
{
    public required ExplorationState ReturnContext { get; init; }

    public required EncounterState Encounter { get; init; }
}
