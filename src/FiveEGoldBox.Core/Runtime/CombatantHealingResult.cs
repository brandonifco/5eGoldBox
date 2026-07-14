using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record CombatantHealingResult
{
    public required CombatantHealthHealingResult
        HealthHealing { get; init; }

    public required CombatantState State { get; init; }
}
