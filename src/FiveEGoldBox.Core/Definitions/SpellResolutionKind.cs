namespace FiveEGoldBox.Core.Definitions;

/// How the spell decides whether it lands.
public enum SpellResolutionKind
{
    /// The caster rolls to hit, as with a weapon.
    SpellAttack,

    /// The target rolls to avoid it.
    SavingThrow,

    /// It simply happens. Magic Missile does not miss.
    Automatic
}
