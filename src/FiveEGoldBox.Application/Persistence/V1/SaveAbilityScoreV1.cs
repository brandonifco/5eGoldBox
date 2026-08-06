namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveAbilityScoreV1
{
    public required SaveAbilityV1 Ability { get; init; }

    public required int Score { get; init; }
}
