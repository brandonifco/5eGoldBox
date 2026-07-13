namespace FiveEGoldBox.Core.Rules;

public sealed record CombatantHealthDamageResult
{
    public required bool IsCriticalHit { get; init; }

    public required HitPointDamageResult HitPointDamage { get; init; }

    public ZeroHitPointDamageResult? ZeroHitPointDamage { get; init; }

    public required CombatantHealthState State { get; init; }
}
