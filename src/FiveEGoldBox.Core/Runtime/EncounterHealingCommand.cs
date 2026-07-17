namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterHealingCommand
{
    public required long ExpectedRevision { get; init; }

    public required string TargetCombatantId { get; init; }

    public required int HealingAmount { get; init; }
}
