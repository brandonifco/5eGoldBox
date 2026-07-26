namespace FiveEGoldBox.Core.Definitions;

/// How a spell is paid for.
///
/// A cantrip costs nothing and may be cast at will; everything else spends a
/// slot of its own level or higher. Kept separate from a bare integer so that
/// "level 0" cannot be mistaken for "no level declared".
public enum SpellCostKind
{
    Cantrip,
    Slot
}
