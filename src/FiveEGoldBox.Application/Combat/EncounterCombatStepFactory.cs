using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Maps Core resolution results onto the step records reported back to callers.
/// Pure record construction shared by player command resolution and automatic
/// turn processing, so both describe the same action the same way.
internal static class EncounterCombatStepFactory
{
    internal static EncounterCombatStepResult CreateMovement(
        EncounterState startingState,
        EncounterMovementResult movement)
    {
        return new EncounterCombatStepResult
        {
            Kind = CombatStepKind.Movement,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = movement.State.Revision,
            ActorCombatantId = movement.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<EncounterCombatDieRoll>(),
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

    /// isOpportunityAttack rides on the ordinary weapon-attack step rather
    /// than getting a CombatStepKind of its own: mechanically it *is* a
    /// weapon attack, and every switch over the kind — narration, the
    /// journal, the transcripts — would otherwise need a new arm to say
    /// the same thing. The flag is only there so a client can tell the
    /// player it was a free hit rather than an ordinary one.
    internal static EncounterCombatStepResult CreateWeaponAttack(
        EncounterState startingState,
        EncounterWeaponAttackResult attack,
        IReadOnlyList<EncounterCombatDieRoll> dice,
        bool isOpportunityAttack = false)
    {
        return new EncounterCombatStepResult
        {
            Kind = CombatStepKind.WeaponAttack,
            IsOpportunityAttack = isOpportunityAttack,
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

    internal static EncounterCombatStepResult CreateSpellAttack(
        EncounterState startingState,
        EncounterSpellCastResult spellAttack,
        IReadOnlyList<EncounterCombatDieRoll> dice)
    {
        return new EncounterCombatStepResult
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

    internal static EncounterCombatStepResult CreateDisengage(
        EncounterState startingState,
        EncounterDisengageResult disengage)
    {
        return new EncounterCombatStepResult
        {
            Kind = CombatStepKind.Disengage,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = disengage.State.Revision,
            ActorCombatantId = disengage.ActorCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<EncounterCombatDieRoll>(),
            Movement = null,
            WeaponAttack = null,
            SpellAttack = null,
            ConcentrationCheck = null,
            DeathSavingThrow = null,
            TurnAdvancement = null,
            TurnAdvanceReason = null,
            WinningSideId = disengage.State.WinningSideId
        };
    }

    internal static EncounterCombatStepResult CreateDeathSavingThrow(
        EncounterState startingState,
        EncounterDeathSavingThrowResult deathSave,
        IReadOnlyList<EncounterCombatDieRoll> dice)
    {
        return new EncounterCombatStepResult
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

    internal static EncounterCombatStepResult CreateTurnAdvanced(
        EncounterState startingState,
        EncounterTurnAdvancementResult turn,
        EncounterCombatTurnAdvanceReason reason)
    {
        return new EncounterCombatStepResult
        {
            Kind = CombatStepKind.TurnAdvanced,
            StartingEncounterRevision = startingState.Revision,
            ResultingEncounterRevision = turn.State.Revision,
            ActorCombatantId = turn.EndedTurnCombatantId,
            TargetCombatantId = null,
            Dice = Array.Empty<EncounterCombatDieRoll>(),
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
        List<EncounterCombatStepResult> steps,
        EncounterState encounter)
    {
        steps.Add(new EncounterCombatStepResult
        {
            Kind = CombatStepKind.CombatCompleted,
            StartingEncounterRevision = encounter.Revision,
            ResultingEncounterRevision = encounter.Revision,
            ActorCombatantId = null,
            TargetCombatantId = null,
            Dice = Array.Empty<EncounterCombatDieRoll>(),
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

    internal static EncounterCombatDieRoll CreateDie(
        ApplicationRandomRoll roll,
        CombatDiePurpose purpose)
    {
        return new EncounterCombatDieRoll
        {
            Ordinal = roll.Ordinal,
            Sides = roll.Sides,
            Value = roll.Value,
            Purpose = purpose
        };
    }
}
