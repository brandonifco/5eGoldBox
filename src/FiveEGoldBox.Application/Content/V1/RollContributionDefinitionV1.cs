namespace FiveEGoldBox.Application.Content.V1;

internal sealed record RollContributionDefinitionV1
{
    public required RollContributionTargetV1 Target { get; init; }

    public int FlatBonus { get; init; }

    public DamageDiceV1? Dice { get; init; }

    public IReadOnlyList<RollContributionConditionV1> Conditions { get; init; }
        = Array.Empty<RollContributionConditionV1>();
}
