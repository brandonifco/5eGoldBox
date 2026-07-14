namespace FiveEGoldBox.Core.Rules;

public sealed record CombatantHealthHealingResult
{
    public required int HealingAmount { get; init; }

    public required int HitPointsRestored { get; init; }

    public required bool ResetDeathSavingThrows { get; init; }

    public required CombatantHealthState State { get; init; }
}
