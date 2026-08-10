using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Projects the Watchtower combat pipeline's results onto the scenario-agnostic
/// contract callers consume. Nothing in the shapes below is Watchtower-specific
/// — the prefix on the source types reflects which pipeline produced them, not
/// what they mean — so this is a rename-and-reshape, not a translation.
internal static class EncounterCombatResultMapper
{
    internal static CombatResolutionResult ToCombatResolutionResult(
        EncounterCombatResolutionResult source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new CombatResolutionResult(
            ToCombatDecision(source.StartingDecision),
            source.SubmittedIntent is null
                ? null
                : ToCombatIntentReceipt(source.SubmittedIntent),
            source.PriorEncounterRevision,
            source.ResultingEncounterRevision,
            source.RandomValuesConsumedBefore,
            source.RandomValuesConsumedAfter,
            source.PrimaryStep is null
                ? null
                : ToCombatStepResult(source.PrimaryStep),
            source.AutomaticSteps
                .Select(ToCombatStepResult)
                .ToArray(),
            ToCombatDecision(source.ResultingDecision),
            source.State);
    }

    private static CombatDecision ToCombatDecision(
        EncounterCombatDecision source)
    {
        CombatWeaponAttackOption[] weaponAttacks = source.WeaponAttacks
            .Select(ToCombatWeaponAttackOption)
            .ToArray();
        CombatSpellAttackOption[] spellAttacks = source.SpellAttacks
            .Select(ToCombatSpellAttackOption)
            .ToArray();

        return new CombatDecision(
            ToCombatDecisionState(source.State),
            source.EncounterRevision,
            source.ActiveCombatantId,
            source.PendingDeathSavingThrowCombatantId,
            source.Movement is null
                ? null
                : ToCombatMovementOption(source.Movement),
            weaponAttacks,
            spellAttacks,
            source.EndTurn is null
                ? null
                : new CombatEndTurnOption(
                    source.EndTurn.IsAvailable,
                    source.EndTurn.UnavailabilityReason),
            source.WinningSideId);
    }

    private static CombatMovementOption ToCombatMovementOption(
        EncounterCombatMovementOption source)
    {
        return new CombatMovementOption(
            source.IsAvailable,
            source.MovementRemainingFeet,
            source.UnavailabilityReason,
            source.DestinationOptions
                .Select(destination => new CombatMovementDestinationOption(
                    destination.Destination,
                    destination.Path,
                    destination.MovementSpentFeet))
                .ToArray());
    }

    private static CombatWeaponAttackOption ToCombatWeaponAttackOption(
        EncounterCombatWeaponAttackOption source)
    {
        return new CombatWeaponAttackOption(
            source.WeaponId,
            source.IsAvailable,
            source.UnavailabilityReason,
            source.Targets
                .Select(ToCombatTargetOption)
                .ToArray());
    }

    private static CombatSpellAttackOption ToCombatSpellAttackOption(
        EncounterCombatSpellAttackOption source)
    {
        return new CombatSpellAttackOption(
            source.SpellId,
            source.SpellName,
            source.IsAvailable,
            source.UnavailabilityReason,
            source.Targets
                .Select(ToCombatTargetOption)
                .ToArray(),
            source.TargetCombinations
                .Select(combination => new CombatTargetCombinationOption(
                    combination.TargetCombatantIds))
                .ToArray());
    }

    private static CombatTargetOption ToCombatTargetOption(
        EncounterCombatTargetOption target)
    {
        return new CombatTargetOption(
            target.TargetCombatantId,
            target.IsAvailable,
            target.UnavailabilityReason,
            target.AttackRollMode,
            target.DistanceFeet,
            target.SaveAbility,
            target.SaveDc);
    }

    private static CombatStepResult ToCombatStepResult(
        EncounterCombatStepResult source)
    {
        return new CombatStepResult(
            ToCombatStepKind(source.Kind),
            source.StartingEncounterRevision,
            source.ResultingEncounterRevision,
            source.ActorCombatantId,
            source.TargetCombatantId,
            source.Dice
                .Select(die => new CombatDieRoll(
                    die.Ordinal,
                    die.Sides,
                    die.Value,
                    ToCombatDiePurpose(die.Purpose)))
                .ToArray(),
            source.Movement is null
                ? null
                : ToMovementDetail(source.Movement),
            source.WeaponAttack is null
                ? null
                : ToWeaponAttackDetail(source.WeaponAttack),
            source.SpellAttack is null
                ? null
                : ToSpellAttackDetail(source.SpellAttack),
            source.ConcentrationCheck is null
                ? null
                : ToConcentrationCheckDetail(source.ConcentrationCheck),
            source.DeathSavingThrow is null
                ? null
                : ToDeathSavingThrowDetail(source.DeathSavingThrow),
            source.TurnAdvancement is null
                ? null
                : ToTurnAdvancementDetail(source.TurnAdvancement),
            source.TurnAdvanceReason is null
                ? null
                : ToCombatTurnAdvanceReason(source.TurnAdvanceReason.Value),
            source.WinningSideId);
    }

    private static CombatMovementStepDetail ToMovementDetail(
        EncounterMovementResult source)
    {
        return new CombatMovementStepDetail(
            source.StartingPosition,
            source.EndingPosition,
            source.Path,
            source.MovementSpentFeet);
    }

    private static CombatWeaponAttackStepDetail ToWeaponAttackDetail(
        EncounterWeaponAttackResult source)
    {
        AttackRollResult attackRoll = source.Attack.AttackRoll;

        return new CombatWeaponAttackStepDetail(
            source.WeaponId,
            source.DistanceFeet,
            source.LineOfSight.HasLineOfSight,
            source.Cover.CoverLevel,
            source.Cover.ArmorClassBonus,
            attackRoll.RollMode,
            attackRoll.FirstRoll,
            attackRoll.SecondRoll,
            attackRoll.NaturalRoll,
            attackRoll.AttackBonus,
            attackRoll.Total,
            attackRoll.TargetArmorClass,
            attackRoll.Outcome,
            source.Attack.Damage.FinalDamage,
            source.TargetDamage is null
                ? null
                : ToDamagedTargetDetail(source.TargetDamage));
    }

    private static CombatSpellAttackStepDetail ToSpellAttackDetail(
        EncounterSpellCastResult source)
    {
        AttackRollResult? attackRoll = source.AttackRoll;
        SavingThrowResult? savingThrow = source.SavingThrow;

        return new CombatSpellAttackStepDetail(
            source.SpellId,
            ResolveSpellName(source.State, source.ActorCombatantId, source.SpellId),
            source.DistanceFeet,
            attackRoll?.RollMode,
            attackRoll?.FirstRoll,
            attackRoll?.SecondRoll,
            attackRoll?.NaturalRoll,
            attackRoll?.AttackBonus,
            attackRoll?.Total,
            attackRoll?.TargetArmorClass,
            attackRoll?.Outcome,
            savingThrow?.Ability,
            savingThrow?.Test.FirstRoll,
            savingThrow?.Test.Bonus,
            savingThrow?.Test.DifficultyClass,
            savingThrow?.Test.Outcome,
            source.TookEffect,
            source.DamageDealt,
            source.HealingDone,
            source.TargetDamage is null
                ? null
                : ToDamagedTargetDetail(source.TargetDamage),
            source.EffectedCombatantIds);
    }

    /// EncounterSpellCastResult only ever carries a raw SpellId -- Core
    /// doesn't resolve ruleset display names during combat resolution, the
    /// same reason EncounterCombatantDisplayNameResolver exists for
    /// combatants. Unlike that resolver, no separate lookup is needed here:
    /// the caster's own already-resolved SpellAttack (with its real name)
    /// still sits on their CombatProfile within the same State this result
    /// already carries.
    private static string ResolveSpellName(
        EncounterState state,
        string actorCombatantId,
        string spellId)
    {
        EncounterParticipantState actor = state.Participants
            .First(participant => string.Equals(
                participant.Combatant.CombatantId,
                actorCombatantId,
                StringComparison.Ordinal));

        return actor.CombatProfile.SpellAttacks
            .First(spell => string.Equals(
                spell.SpellId,
                spellId,
                StringComparison.Ordinal))
            .SpellName;
    }

    private static CombatConcentrationCheckStepDetail
        ToConcentrationCheckDetail(
            EncounterConcentrationCheckResult source)
    {
        EncounterSavingThrowResult? savingThrow = source.SavingThrow;

        return new CombatConcentrationCheckStepDetail(
            source.CombatantId,
            source.EffectId,
            source.BrokenByIncapacitation,
            savingThrow?.SavingThrow?.Test.FirstRoll,
            savingThrow?.CombinedSavingThrowBonus,
            savingThrow?.SavingThrow?.Test.Total,
            savingThrow?.SavingThrow?.Test.DifficultyClass,
            savingThrow?.SavingThrow?.Test.Outcome,
            source.EffectDropped);
    }

    private static CombatDamagedTargetDetail ToDamagedTargetDetail(
        CombatantDamageResult source)
    {
        HitPointState hitPoints = source.State.Health.HitPoints;

        return new CombatDamagedTargetDetail(
            hitPoints.CurrentHitPoints,
            hitPoints.MaximumHitPoints,
            hitPoints.TemporaryHitPoints,
            source.State.LifecycleState);
    }

    private static CombatDeathSavingThrowStepDetail ToDeathSavingThrowDetail(
        EncounterDeathSavingThrowResult source)
    {
        DeathSavingThrowResult result = source.CombatantDeathSavingThrow
            .HealthDeathSavingThrow.DeathSavingThrow;

        return new CombatDeathSavingThrowStepDetail(
            source.PreviousLifecycleState,
            source.LifecycleState,
            result.FirstRoll,
            result.SecondRoll,
            result.NaturalRoll,
            result.SavingThrowBonus,
            result.Total,
            result.DifficultyClass,
            result.Outcome,
            result.State.SuccessCount,
            result.State.FailureCount,
            result.State.IsStable);
    }

    private static CombatTurnAdvancementStepDetail ToTurnAdvancementDetail(
        EncounterTurnAdvancementResult source)
    {
        return new CombatTurnAdvancementStepDetail(
            source.EndedTurnCombatantId,
            source.ActiveCombatantId,
            source.PreviousRoundNumber,
            source.RoundNumber,
            source.StartedNewRound,
            source.SkippedCombatantIds);
    }

    private static CombatIntentReceipt ToCombatIntentReceipt(
        EncounterCombatIntentReceipt source)
    {
        return new CombatIntentReceipt(
            ToCombatIntentKind(source.Kind),
            source.ExpectedEncounterRevision,
            source.ActorCombatantId,
            source.Path,
            source.WeaponId,
            source.SpellId,
            source.TargetCombatantId);
    }

    private static CombatDecisionState ToCombatDecisionState(
        CombatDecisionState source)
    {
        return source switch
        {
            CombatDecisionState.PlayerDecisionRequired =>
                CombatDecisionState.PlayerDecisionRequired,
            CombatDecisionState.AutomaticProcessingRequired =>
                CombatDecisionState.AutomaticProcessingRequired,
            CombatDecisionState.CombatCompleted =>
                CombatDecisionState.CombatCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat decision state.")
        };
    }

    private static CombatStepKind ToCombatStepKind(
        CombatStepKind source)
    {
        return source switch
        {
            CombatStepKind.Movement => CombatStepKind.Movement,
            CombatStepKind.WeaponAttack => CombatStepKind.WeaponAttack,
            CombatStepKind.SpellAttack => CombatStepKind.SpellAttack,
            CombatStepKind.DeathSavingThrow =>
                CombatStepKind.DeathSavingThrow,
            CombatStepKind.TurnAdvanced => CombatStepKind.TurnAdvanced,
            CombatStepKind.CombatCompleted =>
                CombatStepKind.CombatCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat step kind.")
        };
    }

    private static CombatDiePurpose ToCombatDiePurpose(
        CombatDiePurpose source)
    {
        return source switch
        {
            CombatDiePurpose.AttackRoll => CombatDiePurpose.AttackRoll,
            CombatDiePurpose.DamageRoll => CombatDiePurpose.DamageRoll,
            CombatDiePurpose.DeathSavingThrow =>
                CombatDiePurpose.DeathSavingThrow,
            CombatDiePurpose.SavingThrow =>
                CombatDiePurpose.SavingThrow,
            CombatDiePurpose.EffectRoll =>
                CombatDiePurpose.EffectRoll,
            CombatDiePurpose.ConcentrationSavingThrow =>
                CombatDiePurpose.ConcentrationSavingThrow,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat die purpose.")
        };
    }

    private static CombatIntentKind ToCombatIntentKind(
        CombatIntentKind source)
    {
        return source switch
        {
            CombatIntentKind.Move => CombatIntentKind.Move,
            CombatIntentKind.WeaponAttack =>
                CombatIntentKind.WeaponAttack,
            CombatIntentKind.SpellAttack =>
                CombatIntentKind.SpellAttack,
            CombatIntentKind.EndTurn => CombatIntentKind.EndTurn,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat intent kind.")
        };
    }

    /// The raider-specific name becomes the generic "an enemy finished its
    /// turn"; every other reason is already scenario-neutral.
    private static CombatTurnAdvanceReason ToCombatTurnAdvanceReason(
        EncounterCombatTurnAdvanceReason source)
    {
        return source switch
        {
            EncounterCombatTurnAdvanceReason.PlayerEndTurn =>
                CombatTurnAdvanceReason.PlayerEndTurn,
            EncounterCombatTurnAdvanceReason.StableParticipant =>
                CombatTurnAdvanceReason.StableParticipant,
            EncounterCombatTurnAdvanceReason.DyingParticipantAfterSave =>
                CombatTurnAdvanceReason.DyingParticipantAfterSave,
            EncounterCombatTurnAdvanceReason.NoProductiveEnemyAction =>
                CombatTurnAdvanceReason.NoProductiveEnemyAction,
            EncounterCombatTurnAdvanceReason.RaiderTurnCompleted =>
                CombatTurnAdvanceReason.EnemyTurnCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat turn advance reason.")
        };
    }
}
