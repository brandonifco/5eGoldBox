using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record EncounterCombatSpellAttackOption
{
    public required string SpellId { get; init; }

    public required string SpellName { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public required IReadOnlyList<EncounterCombatTargetOption> Targets { get; init; }

    /// Legal sets of two or more targets, for a spell that can reach more
    /// than one. Empty for a spell that only ever names one — everything
    /// that can be cast at all is already on Targets.
    public IReadOnlyList<EncounterCombatTargetCombinationOption>
        TargetCombinations
    { get; init; } = Array.Empty<EncounterCombatTargetCombinationOption>();
}
