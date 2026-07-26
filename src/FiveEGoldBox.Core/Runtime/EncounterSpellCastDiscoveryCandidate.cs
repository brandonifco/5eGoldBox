namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellCastDiscoveryCandidate
{
    public required string ActionOptionId { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }
}
