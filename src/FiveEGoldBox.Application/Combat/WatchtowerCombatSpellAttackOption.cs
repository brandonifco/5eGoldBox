using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatSpellAttackOption
{
    public required string SpellId { get; init; }

    public required bool IsAvailable { get; init; }

    public required EncounterActionUnavailabilityReason UnavailabilityReason { get; init; }

    public required IReadOnlyList<WatchtowerCombatTargetOption> Targets { get; init; }
}
