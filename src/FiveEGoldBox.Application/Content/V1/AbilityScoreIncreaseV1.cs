namespace FiveEGoldBox.Application.Content.V1;

internal sealed record AbilityScoreIncreaseV1
{
    public required AbilityV1 Ability { get; init; }

    public required int Amount { get; init; }
}
