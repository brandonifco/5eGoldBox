namespace FiveEGoldBox.Application.Combat;

public sealed record CombatSpellAttackIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string SpellId { get; init; }

    public required string TargetCombatantId { get; init; }
}
