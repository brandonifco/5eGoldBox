using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// Whether this caster may cast this spell at this target, right now.
///
/// Deliberately the same shape as the weapon equivalent, and deliberately
/// reusing the same line-of-sight and cover rules: a spell that has to see its
/// target sees it the way an arrow does. What differs is what the caster
/// rolls, and that is decided here rather than by the caller.
public static class EncounterSpellPrerequisiteRules
{
    private const int TouchRangeFeet = 5;

    public static EncounterSpellPrerequisiteEvaluation Evaluate(
        EncounterState state,
        string actorCombatantId,
        string targetCombatantId,
        string spellId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(actorCombatantId);
        ArgumentNullException.ThrowIfNull(targetCombatantId);
        ArgumentNullException.ThrowIfNull(spellId);

        EncounterRules.ValidateState(state);

        if (state.LifecycleState != EncounterLifecycleState.Active)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.EncounterCompleted);
        }

        EncounterParticipantState? actor =
            FindParticipant(state, actorCombatantId);

        if (actor is null)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.ActorNotParticipant);
        }

        EncounterParticipantState? target =
            FindParticipant(state, targetCombatantId);

        if (target is null)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.TargetNotParticipant);
        }

        if (!string.Equals(
            actorCombatantId,
            state.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.ActorNotActive);
        }

        if (actor.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.ActorCannotAct);
        }

        SpellAttack? spell = actor.CombatProfile.SpellAttacks
            .FirstOrDefault(candidate => string.Equals(
                candidate.SpellId,
                spellId,
                StringComparison.Ordinal));

        if (spell is null)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.SpellUnavailable);
        }

        EncounterActionUnavailabilityReason? timingProblem =
            FindTimingProblem(actor, spell);

        if (timingProblem is not null)
        {
            return Unavailable(timingProblem.Value);
        }

        if (!HasSlotFor(actor, spell))
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.SpellSlotUnavailable);
        }

        // Healing is cast on an ally; everything else here is cast at an
        // enemy. Whether a spell helps or harms is read from what it does
        // rather than declared twice.
        if (IsHelpful(spell) != string.Equals(
            actor.SideId,
            target.SideId,
            StringComparison.Ordinal))
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.TargetNotHostile);
        }

        if (!IsHelpful(spell) && target.Combatant.IsTerminal)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.TargetCannotBeAttacked);
        }

        int distanceFeet = CalculateDistanceFeet(
            actor.Position,
            target.Position);

        if (distanceFeet > ReachOf(spell))
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.TargetOutOfRange,
                distanceFeet);
        }

        EncounterLineOfSightResult lineOfSight =
            EncounterLineOfSightRules.Evaluate(
                state.Battlefield,
                actor.Position,
                target.Position);

        if (!lineOfSight.HasLineOfSight)
        {
            return Unavailable(
                EncounterActionUnavailabilityReason.LineOfSightBlocked,
                distanceFeet,
                lineOfSight);
        }

        return new EncounterSpellPrerequisiteEvaluation
        {
            IsLegal = true,
            UnavailabilityReason =
                EncounterActionUnavailabilityReason.None,
            AttackRollMode = ResolveAttackRollMode(spell, target),
            DistanceFeet = distanceFeet,
            LineOfSight = lineOfSight,
            Cover = EncounterCoverRules.Evaluate(
                state.Battlefield,
                lineOfSight)
        };
    }

    /// Only a spell the caster rolls to hit has a roll mode. The target's own
    /// state contributes exactly as it does to a weapon attack.
    private static D20RollMode? ResolveAttackRollMode(
        SpellAttack spell,
        EncounterParticipantState target)
    {
        if (spell.Resolution != SpellResolutionKind.SpellAttack)
        {
            return null;
        }

        return D20Rules.ResolveRollMode(
            hasAdvantage: target.Combatant.LifecycleState
                is CombatantLifecycleState.Dying
                    or CombatantLifecycleState.Stable,
            hasDisadvantage: false);
    }

    private static EncounterActionUnavailabilityReason? FindTimingProblem(
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        return spell.CastingTime switch
        {
            SpellCastingTime.Action =>
                actor.TurnResources.HasActionAvailable
                    ? null
                    : EncounterActionUnavailabilityReason.ActionUnavailable,
            SpellCastingTime.BonusAction =>
                actor.TurnResources.HasBonusActionAvailable
                    ? null
                    : EncounterActionUnavailabilityReason
                        .BonusActionUnavailable,
            _ => EncounterActionUnavailabilityReason.UnsupportedTiming
        };
    }

    /// A cantrip costs nothing, so there is nothing to be out of.
    private static bool HasSlotFor(
        EncounterParticipantState actor,
        SpellAttack spell)
    {
        if (spell.SlotResourceId is null)
        {
            return true;
        }

        return actor.CombatProfile.Resources.Any(resource =>
            string.Equals(
                resource.ResourceId,
                spell.SlotResourceId,
                StringComparison.Ordinal)
            && resource.Remaining > 0);
    }

    private static bool IsHelpful(
        SpellAttack spell)
    {
        return spell.Effects.Any(effect =>
            effect.Kind == SpellEffectKind.Healing);
    }

    private static int ReachOf(
        SpellAttack spell)
    {
        return spell.RangeKind switch
        {
            SpellRangeKind.Touch => TouchRangeFeet,
            SpellRangeKind.Ranged => spell.RangeFeet ?? 0,
            _ => 0
        };
    }

    private static EncounterParticipantState? FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.FirstOrDefault(
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static int CalculateDistanceFeet(
        GridPosition from,
        GridPosition to)
    {
        return Math.Max(
            Math.Abs(from.X - to.X),
            Math.Abs(from.Y - to.Y)) * 5;
    }

    private static EncounterSpellPrerequisiteEvaluation Unavailable(
        EncounterActionUnavailabilityReason reason,
        int? distanceFeet = null,
        EncounterLineOfSightResult? lineOfSight = null)
    {
        return new EncounterSpellPrerequisiteEvaluation
        {
            IsLegal = false,
            UnavailabilityReason = reason,
            AttackRollMode = null,
            DistanceFeet = distanceFeet,
            LineOfSight = lineOfSight
        };
    }
}
