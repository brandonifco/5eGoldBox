namespace FiveEGoldBox.Core.Rules;

public sealed record AttackResolutionResult
{
    public required AttackRollResult AttackRoll { get; init; }

    public required AttackDamageResolutionResult Damage { get; init; }
}