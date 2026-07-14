using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record CombatantDamageResult
{
    public required CombatantHealthDamageResult
        HealthDamage { get; init; }

    public required CombatantState State { get; init; }
}
