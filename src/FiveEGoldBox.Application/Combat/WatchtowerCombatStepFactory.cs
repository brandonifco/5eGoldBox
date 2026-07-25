using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Maps Core resolution results onto the step records reported back to callers.
/// Pure record construction shared by player command resolution and automatic
/// turn processing, so both describe the same action the same way.
internal static class WatchtowerCombatStepFactory
{
    internal static WatchtowerCombatStepResult CreateMovement(
        EncounterState startingState,
        EncounterMovementResult movement)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.Movement,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = movement.State.Revision,
            ActorCombatantId = movement.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = movement,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = movement.State.WinningSideId
        };
    }

    internal static WatchtowerCombatStepResult CreateWeaponAttack(
        EncounterState startingState,
        EncounterWeaponAttackResult attack,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.WeaponAttack,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = attack.State.Revision,
            ActorCombatantId = attack.ActorCombatantId,
            TargetCombatantId = attack.TargetCombatantId,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = attack,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = attack.State.WinningSideId
        };
    }

    internal static WatchtowerCombatStepResult CreateDeathSavingThrow(
        EncounterState startingState,
        EncounterDeathSavingThrowResult deathSave,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.DeathSavingThrow,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = deathSave.State.Revision,
            ActorCombatantId = deathSave.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = deathSave,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = deathSave.State.WinningSideId
        };
    }

    internal static WatchtowerCombatStepResult CreateTurnAdvanced(
        EncounterState startingState,
        EncounterTurnAdvancementResult turn,
        WatchtowerCombatTurnAdvanceReason reason)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.TurnAdvanced,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = turn.State.Revision,
            ActorCombatantId = turn.EndedTurnCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = turn,
            TurnAdvanceReason = reason,
            WinningSideId = turn.State.WinningSideId
        };
    }

    internal static void AppendCompletion(
        List<WatchtowerCombatStepResult> steps,
        EncounterState encounter)
    {
        if (steps.Count > 0
            && steps[^1].Kind
                == WatchtowerCombatStepKind.CombatCompleted)
        {
            return;
        }

        steps.Add(new WatchtowerCombatStepResult
        {
            Kind = WatchtowerCombatStepKind.CombatCompleted,
            StartingEncounterRevision = encounter.Revision,
            ResultingEncounterRevision = encounter.Revision,
            ActorCombatantId = null,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = encounter.WinningSideId
        });
    }

    internal static WatchtowerCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        WatchtowerCombatDiePurpose purpose)
    {
        return new WatchtowerCombatDieRoll
        {
            Ordinal = roll.Ordinal,
            Sides = roll.Sides,
            Value = roll.Value,
            Purpose = purpose
        };
    }
}
