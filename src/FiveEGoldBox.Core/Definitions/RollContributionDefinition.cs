using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

/// Something that changes a roll.
///
/// This is the seam the whole design turns on. Bless adds a d4 to attack rolls
/// and saving throws; Rage adds a flat bonus to melee damage; a fighting style
/// adds two to attack rolls with a bow. They differ in which roll and how
/// much, not in kind — so one type describes all of them, and whatever
/// installs it is somebody else's business.
public sealed record RollContributionDefinition
{
    public required RollContributionTarget Target { get; init; }

    /// Added outright.
    public int FlatBonus { get; init; }

    /// Rolled and added. The caller supplies the dice, as it does everywhere
    /// else, which is why a roll has to be asked what it needs before it is
    /// made.
    public DamageDice? Dice { get; init; }

    /// What has to be true for this to count. Every one of them, or the
    /// contribution stays out of the roll. Empty means always, which is Bless
    /// and most of what will follow it.
    public IReadOnlyList<RollContributionCondition> Conditions { get; init; }
        = Array.Empty<RollContributionCondition>();
}
