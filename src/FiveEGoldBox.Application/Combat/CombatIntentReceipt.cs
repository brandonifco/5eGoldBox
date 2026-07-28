using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// The command a caller submitted, echoed back alongside its result.
public sealed record CombatIntentReceipt
{
    internal CombatIntentReceipt(
        CombatIntentKind kind,
        long expectedEncounterRevision,
        string actorCombatantId,
        IReadOnlyList<GridPosition> path,
        string? weaponId,
        string? spellId,
        string? targetCombatantId)
    {
        ArgumentNullException.ThrowIfNull(path);

        Kind = kind;
        ExpectedEncounterRevision = expectedEncounterRevision;
        ActorCombatantId = actorCombatantId;
        Path = Array.AsReadOnly(path.ToArray());
        WeaponId = weaponId;
        SpellId = spellId;
        TargetCombatantId = targetCombatantId;
    }

    public CombatIntentKind Kind { get; }

    public long ExpectedEncounterRevision { get; }

    public string ActorCombatantId { get; }

    /// Populated for movement; empty for every other kind.
    public IReadOnlyList<GridPosition> Path { get; }

    public string? WeaponId { get; }

    public string? SpellId { get; }

    public string? TargetCombatantId { get; }
}
