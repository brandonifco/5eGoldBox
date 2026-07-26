using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// Everything contributing to one roll, gathered before it is made.
///
/// The dice are flattened to one entry per die rather than kept as counts,
/// because the caller's job is to roll exactly this many and hand the values
/// back in order — and because two effects each adding a d4 is the same
/// request as one effect adding two.
public sealed record RollContributionSet
{
    public required RollContributionTarget Target { get; init; }

    /// Added outright, whatever the dice come to.
    public required int FlatBonus { get; init; }

    /// What the caller must roll before the roll can be resolved. This is the
    /// whole reason a roll has to be asked what it needs: a contribution that
    /// adds dice changes the roll's arity, and the caller owns randomness.
    public IReadOnlyList<DieType> RequiredDice { get; init; }
        = Array.Empty<DieType>();

    /// Whether the roll is affected at all. A roll nothing contributes to is
    /// the common case, and it must cost the caller nothing.
    public bool IsEmpty =>
        FlatBonus == 0
        && RequiredDice.Count == 0;

    public static RollContributionSet None(
        RollContributionTarget target)
    {
        return new RollContributionSet
        {
            Target = target,
            FlatBonus = 0
        };
    }
}
