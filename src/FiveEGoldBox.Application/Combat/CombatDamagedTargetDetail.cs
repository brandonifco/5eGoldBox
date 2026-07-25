using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// A target's condition after taking damage.
public sealed record CombatDamagedTargetDetail
{
    internal CombatDamagedTargetDetail(
        int currentHitPoints,
        int maximumHitPoints,
        int temporaryHitPoints,
        CombatantLifecycleState lifecycleState)
    {
        CurrentHitPoints = currentHitPoints;
        MaximumHitPoints = maximumHitPoints;
        TemporaryHitPoints = temporaryHitPoints;
        LifecycleState = lifecycleState;
    }

    public int CurrentHitPoints { get; }

    public int MaximumHitPoints { get; }

    public int TemporaryHitPoints { get; }

    public CombatantLifecycleState LifecycleState { get; }
}
