namespace FiveEGoldBox.Core.Runtime;

/// One reaction a single step of movement has provoked: who gets the free
/// swing, and with which of their weapons.
///
/// Deliberately not the attack's outcome, or even its dice — this is the
/// answer to "does anything trigger here", produced by a pure query with no
/// randomness in it, exactly like every other Core rule. The caller rolls
/// the dice and resolves the attack through the ordinary weapon-attack path
/// with EncounterWeaponAttackTiming.Reaction.
public sealed record EncounterOpportunityAttack
{
    public required string AttackerCombatantId { get; init; }

    /// The melee weapon the attacker would swing. Chosen by the rule rather
    /// than left to the caller so the trigger and the attack cannot end up
    /// disagreeing about whether the mover was ever in reach.
    public required string WeaponId { get; init; }

    /// The square the mover was leaving when this triggered. Carried so a
    /// client can say where the free attack happened, and so a caller
    /// resolving several provocations from one step can tell them apart.
    public required GridPosition FromPosition { get; init; }
}
