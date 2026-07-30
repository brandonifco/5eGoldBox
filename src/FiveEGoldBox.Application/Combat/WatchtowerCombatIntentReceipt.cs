using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatIntentReceipt
{
    public required CombatIntentKind Kind { get; init; }

    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required IReadOnlyList<GridPosition> Path { get; init; }

    public string? WeaponId { get; init; }

    public string? SpellId { get; init; }

    public string? TargetCombatantId { get; init; }
}
