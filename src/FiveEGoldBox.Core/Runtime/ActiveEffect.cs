using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Runtime;

/// An effect currently sitting on a combatant.
///
/// Carries its contributions rather than an identifier to look up, for the
/// same reason a weapon attack carries resolved damage: the encounter should
/// not need a ruleset in hand to work out what a roll comes to.
public sealed record ActiveEffect
{
    public required string EffectId { get; init; }

    /// Who is sustaining it. When they stop, it ends.
    public required string SourceCombatantId { get; init; }

    public required int RemainingRounds { get; init; }

    /// Whether the source has to concentrate to keep it up.
    public required bool RequiresConcentration { get; init; }

    public IReadOnlyList<RollContributionDefinition> Contributions { get; init; }
        = Array.Empty<RollContributionDefinition>();
}
