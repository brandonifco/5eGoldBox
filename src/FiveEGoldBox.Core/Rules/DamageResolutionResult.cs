namespace FiveEGoldBox.Core.Rules;

public sealed record DamageResolutionResult
{
    public required DamageRollResult DamageRoll { get; init; }

    public required IReadOnlyList<DamageResponseType> ResponseTypes { get; init; }

    public required int FinalDamage { get; init; }
}
