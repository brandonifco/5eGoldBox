namespace FiveEGoldBox.Application.Content.V1;

internal sealed record EncounterOutcomeDefinitionV1
{
    public required string VictoryProgressId { get; init; }

    public required string DefeatProgressId { get; init; }
}
