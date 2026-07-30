namespace FiveEGoldBox.Application.Content.V1;

internal sealed record EncounterDefinitionV1
{
    public required string EncounterId { get; init; }

    public required string BattlefieldId { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required string PartySideId { get; init; }

    public required IReadOnlyList<GridPositionV1> BlockedPositions { get; init; }

    public required IReadOnlyList<GridPositionV1> PartyStartingPositions { get; init; }

    public required IReadOnlyList<EncounterCombatantDefinitionV1> Combatants { get; init; }

    public required EncounterOutcomeDefinitionV1 Outcome { get; init; }
}
