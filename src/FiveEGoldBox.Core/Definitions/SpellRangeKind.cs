namespace FiveEGoldBox.Core.Definitions;

public enum SpellRangeKind
{
    /// Affects only the caster, and needs no target.
    SelfOnly,

    /// Reaches an adjacent creature.
    Touch,

    /// Reaches a stated distance.
    Ranged
}
