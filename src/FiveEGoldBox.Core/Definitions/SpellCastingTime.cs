namespace FiveEGoldBox.Core.Definitions;

/// Which part of a turn casting the spell uses.
///
/// These are the timings the encounter already models; a spell cast as a
/// reaction is declared here but nothing offers reactions yet beyond
/// opportunity attacks.
public enum SpellCastingTime
{
    Action,
    BonusAction,
    Reaction
}
