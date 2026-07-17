namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDamageCommand
{
    public required long ExpectedRevision { get; init; }

    public required string TargetCombatantId { get; init; }

    public required int DamageAmount { get; init; }

    public required bool IsCriticalHit { get; init; }
}
