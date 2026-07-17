namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterTurnAdvancementCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }
}
