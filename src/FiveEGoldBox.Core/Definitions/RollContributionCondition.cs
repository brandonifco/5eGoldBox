namespace FiveEGoldBox.Core.Definitions;

/// A question a contribution asks before it counts.
///
/// Bless asks nothing — it applies to every attack the blessed creature
/// makes. Sneak Attack asks two things, and a contribution that names several
/// needs all of them to hold. What is deliberately absent is a way to write
/// "or": the one disjunction 5e actually needs lives inside a single named
/// condition below, because an expression language is a much larger thing
/// than the two features that would use it.
public enum RollContributionCondition
{
    /// The attacker had advantage on the roll, or the target had an enemy of
    /// its own adjacent to it — and in either case the attacker did not have
    /// disadvantage.
    ///
    /// This is 5e's Sneak Attack trigger stated as one question, because that
    /// is what it is: advantage alone qualifies, and a crowded target
    /// qualifies as long as nothing is spoiling the shot.
    AdvantageOrAdjacentEnemy,

    /// The attack was made with a finesse weapon or a ranged one. A rogue
    /// with a greatsword is still a rogue, but not a sneaking one.
    FinesseOrRangedWeapon
}
