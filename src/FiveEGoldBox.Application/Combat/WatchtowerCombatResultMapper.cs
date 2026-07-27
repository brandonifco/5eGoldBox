using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Projects the Watchtower combat pipeline's results onto the scenario-agnostic
/// contract callers consume. Nothing in the shapes below is Watchtower-specific
/// — the prefix on the source types reflects which pipeline produced them, not
/// what they mean — so this is a rename-and-reshape, not a translation.
internal static class WatchtowerCombatResultMapper
{
    internal static CombatResolutionResult ToCombatResolutionResult(
        WatchtowerCombatResolutionResult source)
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
        WatchtowerCombatDecision source)
    {
        CombatWeaponAttackOption[] weaponAttacks = source.WeaponAttacks
            .Select(ToCombatWeaponAttackOption)
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
            source.EndTurn is null
                ? null
                : new CombatEndTurnOption(
                    source.EndTurn.IsAvailable,
                    source.EndTurn.UnavailabilityReason),
            source.WinningSideId);
    }

    private static CombatMovementOption ToCombatMovementOption(
        WatchtowerCombatMovementOption source)
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
        WatchtowerCombatWeaponAttackOption source)
    {
        return new CombatWeaponAttackOption(
            source.WeaponId,
            source.IsAvailable,
            source.UnavailabilityReason,
            source.Targets
                .Select(target => new CombatTargetOption(
                    target.TargetCombatantId,
                    target.IsAvailable,
                    target.UnavailabilityReason,
                    target.AttackRollMode,
                    target.DistanceFeet))
                .ToArray());
    }

    private static CombatStepResult ToCombatStepResult(
        WatchtowerCombatStepResult source)
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
        WatchtowerCombatIntentReceipt source)
    {
        return new CombatIntentReceipt(
            ToCombatIntentKind(source.Kind),
            source.ExpectedEncounterRevision,
            source.ActorCombatantId,
            source.Path,
            source.WeaponId,
            source.TargetCombatantId);
    }

    private static CombatDecisionState ToCombatDecisionState(
        WatchtowerCombatDecisionState source)
    {
        return source switch
        {
            WatchtowerCombatDecisionState.PlayerDecisionRequired =>
                CombatDecisionState.PlayerDecisionRequired,
            WatchtowerCombatDecisionState.AutomaticProcessingRequired =>
                CombatDecisionState.AutomaticProcessingRequired,
            WatchtowerCombatDecisionState.CombatCompleted =>
                CombatDecisionState.CombatCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat decision state.")
        };
    }

    private static CombatStepKind ToCombatStepKind(
        WatchtowerCombatStepKind source)
    {
        return source switch
        {
            WatchtowerCombatStepKind.Movement => CombatStepKind.Movement,
            WatchtowerCombatStepKind.WeaponAttack => CombatStepKind.WeaponAttack,
            WatchtowerCombatStepKind.DeathSavingThrow =>
                CombatStepKind.DeathSavingThrow,
            WatchtowerCombatStepKind.TurnAdvanced => CombatStepKind.TurnAdvanced,
            WatchtowerCombatStepKind.CombatCompleted =>
                CombatStepKind.CombatCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat step kind.")
        };
    }

    private static CombatDiePurpose ToCombatDiePurpose(
        WatchtowerCombatDiePurpose source)
    {
        return source switch
        {
            WatchtowerCombatDiePurpose.AttackRoll => CombatDiePurpose.AttackRoll,
            WatchtowerCombatDiePurpose.DamageRoll => CombatDiePurpose.DamageRoll,
            WatchtowerCombatDiePurpose.DeathSavingThrow =>
                CombatDiePurpose.DeathSavingThrow,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat die purpose.")
        };
    }

    private static CombatIntentKind ToCombatIntentKind(
        WatchtowerCombatIntentKind source)
    {
        return source switch
        {
            WatchtowerCombatIntentKind.Move => CombatIntentKind.Move,
            WatchtowerCombatIntentKind.WeaponAttack =>
                CombatIntentKind.WeaponAttack,
            WatchtowerCombatIntentKind.EndTurn => CombatIntentKind.EndTurn,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat intent kind.")
        };
    }

    /// The raider-specific name becomes the generic "an enemy finished its
    /// turn"; every other reason is already scenario-neutral.
    private static CombatTurnAdvanceReason ToCombatTurnAdvanceReason(
        WatchtowerCombatTurnAdvanceReason source)
    {
        return source switch
        {
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn =>
                CombatTurnAdvanceReason.PlayerEndTurn,
            WatchtowerCombatTurnAdvanceReason.StableParticipant =>
                CombatTurnAdvanceReason.StableParticipant,
            WatchtowerCombatTurnAdvanceReason.DyingParticipantAfterSave =>
                CombatTurnAdvanceReason.DyingParticipantAfterSave,
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction =>
                CombatTurnAdvanceReason.NoProductiveEnemyAction,
            WatchtowerCombatTurnAdvanceReason.RaiderTurnCompleted =>
                CombatTurnAdvanceReason.EnemyTurnCompleted,
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Unsupported combat turn advance reason.")
        };
    }
}
