namespace FiveEGoldBox.Application.Content.V1;

/// One scenario as it exists on disk. Unlike a ruleset pack, a scenario is
/// authored as one cohesive adventure rather than merged from several files
/// -- there is no "additive scenario pack" concept, so every field here is
/// required, the way ScenarioDefinition itself declares them.
internal sealed record ScenarioPackV1
{
    public required int FormatVersion { get; init; }

    public required string ScenarioId { get; init; }

    public required string DisplayName { get; init; }

    public required string RulesetId { get; init; }

    public required string StartingLocationId { get; init; }

    public required ScenarioProgressDefinitionV1 Progress { get; init; }

    public required PartyRequirementDefinitionV1 PartyRequirement { get; init; }

    public required IReadOnlyList<ScenarioLocationDefinitionV1> Locations { get; init; }

    public required IReadOnlyList<TravelRouteDefinitionV1> Routes { get; init; }

    public required IReadOnlyList<EncounterDefinitionV1> Encounters { get; init; }

    public required IReadOnlyList<ScenarioTriggerDefinitionV1> Triggers { get; init; }

    public required IReadOnlyList<ScenarioDecisionDefinitionV1> Decisions { get; init; }

    public IReadOnlyList<ShopDefinitionV1> Shops { get; init; } =
        Array.Empty<ShopDefinitionV1>();
}
