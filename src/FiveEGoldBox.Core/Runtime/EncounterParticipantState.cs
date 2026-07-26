using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterParticipantState
{
    public required CombatantState Combatant { get; init; }

    public required EncounterCombatProfile CombatProfile { get; init; }

    public required string SideId { get; init; }

    public required CombatTurnResources TurnResources { get; init; }

    public required GridPosition Position { get; init; }

    /// Effects currently sitting on this combatant.
    public IReadOnlyList<ActiveEffect> ActiveEffects { get; init; }
        = Array.Empty<ActiveEffect>();

    /// The effect this combatant is sustaining, if any. At most one, which is
    /// what makes concentration a constraint rather than a label.
    public string? ConcentratingOnEffectId { get; init; }
}
