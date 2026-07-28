namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatSpellAttackIntent
{
    public required long ExpectedEncounterRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string SpellId { get; init; }

    public required string TargetCombatantId { get; init; }
}
