using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatSpellAttackOption
{
    public required string SpellId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public required IReadOnlyList<WatchtowerCombatTargetOption> Targets { get; init; }

    /// Legal sets of two or more targets, for a spell that can reach more
    /// than one. Empty for a spell that only ever names one — everything
    /// that can be cast at all is already on Targets.
    public IReadOnlyList<WatchtowerCombatTargetCombinationOption>
        TargetCombinations
    { get; init; } = Array.Empty<WatchtowerCombatTargetCombinationOption>();
}
