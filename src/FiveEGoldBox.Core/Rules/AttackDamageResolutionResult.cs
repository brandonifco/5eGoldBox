using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Rules;

public sealed record AttackDamageResolutionResult
{
    public required AttackRollOutcome AttackOutcome { get; init; }

    public DamageDice? DamageDice { get; init; }

    public DamageRollResult? DamageRoll { get; init; }

    public required IReadOnlyList<DamageResponseType> ResponseTypes { get; init; }

    public required int FinalDamage { get; init; }
}
