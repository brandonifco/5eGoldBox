namespace FiveEGoldBox.Core.Rules;

public sealed record HitPointDamageResult
{
    public required int DamageAmount { get; init; }

    public required int DamageAbsorbedByTemporaryHitPoints { get; init; }

    public required int DamageAppliedToCurrentHitPoints { get; init; }

    public required int DamageRemainingAfterReachingZeroHitPoints { get; init; }

    public required bool StartedAtZeroHitPoints { get; init; }

    public required HitPointState State { get; init; }

    public bool ReducedToZeroHitPoints =>
        !StartedAtZeroHitPoints
        && State.IsAtZeroHitPoints;
}
