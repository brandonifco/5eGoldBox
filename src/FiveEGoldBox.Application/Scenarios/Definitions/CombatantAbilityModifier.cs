using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// One ability's modifier for a scenario-authored combatant.
///
/// The opposition does not go through character creation, so it has no scores
/// to derive these from. A scenario states them directly, the way a stat block
/// does.
internal sealed record CombatantAbilityModifier
{
    internal required Ability Ability { get; init; }

    internal required int Modifier { get; init; }
}
