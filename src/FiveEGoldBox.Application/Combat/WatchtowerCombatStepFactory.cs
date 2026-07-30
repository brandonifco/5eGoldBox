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
            Kind = CombatStepKind.Movement,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = movement.State.Revision,
            ActorCombatantId = movement.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = movement,
            WeaponAttack = null,
            SpellAttack = null,
            ConcentrationCheck = null,
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
            Kind = CombatStepKind.WeaponAttack,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = attack.State.Revision,
            ActorCombatantId = attack.ActorCombatantId,
            TargetCombatantId = attack.TargetCombatantId,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = attack,
            SpellAttack = null,
            ConcentrationCheck = attack.ConcentrationCheck,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = attack.State.WinningSideId
        };
    }

    internal static WatchtowerCombatStepResult CreateSpellAttack(
        EncounterState startingState,
        EncounterSpellCastResult spellAttack,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = CombatStepKind.SpellAttack,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = spellAttack.State.Revision,
            ActorCombatantId = spellAttack.ActorCombatantId,
            TargetCombatantId = spellAttack.TargetCombatantId,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = null,
            SpellAttack = spellAttack,
            ConcentrationCheck = spellAttack.ConcentrationCheck,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = spellAttack.State.WinningSideId
        };
    }

    internal static WatchtowerCombatStepResult CreateDeathSavingThrow(
        EncounterState startingState,
        EncounterDeathSavingThrowResult deathSave,
        IReadOnlyList<WatchtowerCombatDieRoll> dice)
    {
        return new WatchtowerCombatStepResult
        {
            Kind = CombatStepKind.DeathSavingThrow,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = deathSave.State.Revision,
            ActorCombatantId = deathSave.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.AsReadOnly(dice.ToArray()),
            Movement = null,
            WeaponAttack = null,
            SpellAttack = null,
            ConcentrationCheck = null,
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
            Kind = CombatStepKind.TurnAdvanced,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = turn.State.Revision,
            ActorCombatantId = turn.EndedTurnCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            SpellAttack = null,
            ConcentrationCheck = null,
            DeathSavingThrow = null,
            TurnAdvancement = turn,
            TurnAdvanceReason = reason,
            WinningSideId = turn.State.WinningSideId
        };
    }

    /// Records that the fight is over.
    ///
    /// Called once, by the automatic processor, which returns as soon as it
    /// has. Both of its callers hand it a list they created empty a line
    /// earlier, so it cannot be asked to append a completion to a run that
    /// already ended.
    internal static void AppendCompletion(
        List<WatchtowerCombatStepResult> steps,
        EncounterState encounter)
    {
        steps.Add(new WatchtowerCombatStepResult
        {
            Kind = CombatStepKind.CombatCompleted,
            StartingEncounterRevision = encounter.Revision,
            ResultingEncounterRevision = encounter.Revision,
            ActorCombatantId = null,
            TargetCombatantId = null,
            Dice = Array.Empty<WatchtowerCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            SpellAttack = null,
            ConcentrationCheck = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = encounter.WinningSideId
        });
    }

    internal static WatchtowerCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        CombatDiePurpose purpose)
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
