using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

/// One ability's modifier for a ruleset-authored monster.
///
/// The opposition does not go through character creation, so it has no scores
/// to derive these from. A ruleset states them directly, the way a stat block
/// does.
public sealed record MonsterAbilityModifier
{
    public required Ability Ability { get; init; }

    public required int Modifier { get; init; }
}
