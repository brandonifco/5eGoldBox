using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record WatchtowerCombatMoveIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }
}
