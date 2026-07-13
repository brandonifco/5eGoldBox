namespace FiveEGoldBox.Core.Rules;

public sealed record ZeroHitPointDamageResult
{
    public required int MaximumHitPoints { get; init; }

    public required int DamageAmount { get; init; }

    public required bool IsCriticalHit { get; init; }

    public required int DeathSavingThrowFailuresCaused { get; init; }

    public required ZeroHitPointDamageOutcome Outcome { get; init; }

    public required DeathSavingThrowState State { get; init; }

    public bool IsDead =>
        Outcome is ZeroHitPointDamageOutcome.Dead
            or ZeroHitPointDamageOutcome.InstantDeath;
}
