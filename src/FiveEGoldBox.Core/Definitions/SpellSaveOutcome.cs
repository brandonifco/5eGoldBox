namespace FiveEGoldBox.Core.Definitions;

/// What a successful saving throw earns the target.
public enum SpellSaveOutcome
{
    /// The spell does nothing at all.
    Negates,

    /// Damage is halved, rounded down.
    HalvesDamage
}
