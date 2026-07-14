namespace FiveEGoldBox.Core.Rules;

public sealed record CombatantHealthDeathSavingThrowResult
{
    public required DeathSavingThrowResult
        DeathSavingThrow { get; init; }

    public required CombatantHealthState State { get; init; }
}
