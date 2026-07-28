using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerCombatStepResult
{
    public required WatchtowerCombatStepKind Kind { get; init; }

    public required long StartingEncounterRevision { get; init; }

    public required long ResultingEncounterRevision { get; init; }

    public string? ActorCombatantId { get; init; }

    public string? TargetCombatantId { get; init; }

    public required IReadOnlyList<WatchtowerCombatDieRoll> Dice { get; init; }

    public EncounterMovementResult? Movement { get; init; }

    public EncounterWeaponAttackResult? WeaponAttack { get; init; }

    public EncounterSpellCastResult? SpellAttack { get; init; }

    /// Set whenever this step damaged a combatant who was concentrating on
    /// something, whichever kind of step did it. Populated regardless of
    /// Kind rather than nested under WeaponAttack or SpellAttack, since
    /// either can trigger one.
    public EncounterConcentrationCheckResult? ConcentrationCheck { get; init; }

    public EncounterDeathSavingThrowResult? DeathSavingThrow { get; init; }

    public EncounterTurnAdvancementResult? TurnAdvancement { get; init; }

    public WatchtowerCombatTurnAdvanceReason? TurnAdvanceReason { get; init; }

    public string? WinningSideId { get; init; }
}
