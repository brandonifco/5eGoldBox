namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDisengageCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }
}
