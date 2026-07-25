using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record EncounterDefinition
{
    internal required string EncounterId { get; init; }

    internal required string BattlefieldId { get; init; }

    internal required int Width { get; init; }

    internal required int Height { get; init; }

    /// The side the campaign's party fights on.
    internal required string PartySideId { get; init; }

    internal required IReadOnlyList<GridPosition> BlockedPositions { get; init; }

    /// Where party members deploy, in order. Must accommodate the largest party
    /// the scenario's requirement permits.
    internal required IReadOnlyList<GridPosition> PartyStartingPositions { get; init; }

    /// Everyone the scenario itself places on the battlefield.
    internal required IReadOnlyList<CombatantDefinition> Combatants { get; init; }
}
