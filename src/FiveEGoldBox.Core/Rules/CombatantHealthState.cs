namespace FiveEGoldBox.Core.Rules;

public sealed record CombatantHealthState
{
    public required HitPointState HitPoints { get; init; }

    public required DeathSavingThrowState DeathSavingThrows { get; init; }

    public required bool IsInstantlyDead { get; init; }

    public bool IsDead =>
        IsInstantlyDead
        || DeathSavingThrows.IsDead;
}
