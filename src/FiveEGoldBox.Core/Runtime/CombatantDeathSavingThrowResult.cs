using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record CombatantDeathSavingThrowResult
{
    public required CombatantHealthDeathSavingThrowResult
        HealthDeathSavingThrow { get; init; }

    public required CombatantState State { get; init; }
}
