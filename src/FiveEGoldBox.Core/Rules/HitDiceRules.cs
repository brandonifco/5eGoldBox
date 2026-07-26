namespace FiveEGoldBox.Core.Rules;

/// Which dice a class may use as its hit die, and what each contributes per
/// level after the first.
///
/// Deliberately narrower than <see cref="DieType"/>: 5e classes use d6 through
/// d12, and there is no d4 or d20 hit die. Both the resolver and ruleset
/// validation read this, so a hit die that would fail when a character is built
/// is refused when the ruleset is loaded instead.
internal static class HitDiceRules
{
    internal static bool IsSupported(
        DieType hitDie)
    {
        return hitDie switch
        {
            DieType.D6
                or DieType.D8
                or DieType.D10
                or DieType.D12 => true,
            _ => false
        };
    }

    internal static int GetFixedHitPointsAfterFirstLevel(
        DieType hitDie)
    {
        return hitDie switch
        {
            DieType.D6 => 4,
            DieType.D8 => 5,
            DieType.D10 => 6,
            DieType.D12 => 7,
            _ => throw new InvalidOperationException(
                $"Unsupported class hit die '{hitDie}'.")
        };
    }
}
