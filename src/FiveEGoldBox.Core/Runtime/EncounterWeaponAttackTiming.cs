namespace FiveEGoldBox.Core.Runtime;

/// Which slice of the action economy a weapon attack is being taken out of.
///
/// Every attack in this engine used to be an Action taken by whoever's turn
/// it is, so the constraint was simply inlined into the prerequisite rules:
/// the actor had to be the active combatant and had to still hold its
/// Action. A reaction is the opposite on both counts — it happens on
/// somebody else's turn, by definition, and spends the Reaction instead.
///
/// Naming the timing rather than adding an "isReaction" flag is deliberate:
/// a Ready action and a Guard-equivalent both resolve through this same
/// path once they exist, and neither is well described by a boolean.
public enum EncounterWeaponAttackTiming
{
    /// The ordinary case: the active combatant spends its Action.
    Action,

    /// Taken on another combatant's turn, spending the Reaction. Nothing in
    /// this enum decides *whether* a reaction is warranted — that is the
    /// triggering rule's job (EncounterOpportunityAttackRules today) — only
    /// what it costs and who is allowed to take it.
    Reaction
}
