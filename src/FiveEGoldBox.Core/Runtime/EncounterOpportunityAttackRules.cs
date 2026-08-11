using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// 5e's opportunity attack, as a pure query: "this combatant is about to
/// step from here to there — does anyone get a free swing?"
///
/// The trigger is *leaving reach*, not moving while threatened. A combatant
/// sidestepping from one square to another that is still within an enemy's
/// reach provokes nothing, which is what makes closing to melee and
/// repositioning inside it safe while walking away is not. Getting this
/// backwards would tax all movement rather than retreat specifically.
///
/// Dice-free and state-free by construction — it answers what triggers, and
/// the caller resolves each one through the ordinary weapon-attack path with
/// EncounterWeaponAttackTiming.Reaction. That split is what lets movement
/// stay a Core rule while the randomness stays in the layer that owns it.
public static class EncounterOpportunityAttackRules
{
    private const int FeetPerGridSquare = 5;
    private const int DefaultMeleeReachFeet = 5;

    /// Every provocation a single step triggers, in participant order so the
    /// result is deterministic — two enemies both losing the same retreating
    /// target must always swing in the same sequence, since each attack can
    /// change what the next one is resolving against.
    ///
    /// Returns empty rather than throwing for a mover who is not a
    /// participant: this is a question, and "nobody reacts to a combatant
    /// who isn't here" is a real answer.
    public static IReadOnlyList<EncounterOpportunityAttack> FindProvocations(
        EncounterState state,
        string moverCombatantId,
        GridPosition fromPosition,
        GridPosition toPosition)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(moverCombatantId))
        {
            throw new ArgumentException(
                "Mover combatant ID is required.",
                nameof(moverCombatantId));
        }

        EncounterRules.ValidateState(state);

        if (state.LifecycleState != EncounterLifecycleState.Active)
        {
            return Array.Empty<EncounterOpportunityAttack>();
        }

        EncounterParticipantState? mover = state.Participants
            .FirstOrDefault(participant => string.Equals(
                participant.Combatant.CombatantId,
                moverCombatantId,
                StringComparison.Ordinal));

        if (mover is null)
        {
            return Array.Empty<EncounterOpportunityAttack>();
        }

        // Disengage is the whole counterplay to this feature existing: without
        // it, opportunity attacks are a flat tax on retreating with no way to
        // answer them. Checked before anything else, so a disengaged mover
        // costs nothing to evaluate.
        if (mover.TurnResources.HasDisengaged)
        {
            return Array.Empty<EncounterOpportunityAttack>();
        }

        // An unconscious combatant being dragged or shoved would still be
        // "moving", but nothing in this engine moves a combatant who cannot
        // act, and an opportunity attack against one is not a thing 5e has.
        if (mover.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            return Array.Empty<EncounterOpportunityAttack>();
        }

        List<EncounterOpportunityAttack> provocations = [];

        foreach (EncounterParticipantState candidate in state.Participants)
        {
            if (TryFindProvocation(
                candidate,
                mover,
                fromPosition,
                toPosition) is EncounterOpportunityAttack provocation)
            {
                provocations.Add(provocation);
            }
        }

        return Array.AsReadOnly(provocations.ToArray());
    }

    private static EncounterOpportunityAttack? TryFindProvocation(
        EncounterParticipantState candidate,
        EncounterParticipantState mover,
        GridPosition fromPosition,
        GridPosition toPosition)
    {
        if (string.Equals(
            candidate.Combatant.CombatantId,
            mover.Combatant.CombatantId,
            StringComparison.Ordinal))
        {
            return null;
        }

        if (string.Equals(
            candidate.SideId,
            mover.SideId,
            StringComparison.Ordinal))
        {
            return null;
        }

        if (candidate.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            return null;
        }

        if (!candidate.TurnResources.HasReactionAvailable)
        {
            return null;
        }

        // Longest reach wins, which matters the moment a reach weapon is
        // authored: a combatant carrying both a halberd and a dagger
        // threatens the halberd's squares, not the dagger's.
        WeaponAttack? weapon = null;
        int bestReachFeet = 0;

        foreach (WeaponAttack candidateWeapon
            in candidate.CombatProfile.WeaponAttacks)
        {
            if (candidateWeapon.AttackKind != WeaponAttackKind.Melee)
            {
                continue;
            }

            int reachFeet =
                candidateWeapon.ReachFeet ?? DefaultMeleeReachFeet;

            if (weapon is null || reachFeet > bestReachFeet)
            {
                weapon = candidateWeapon;
                bestReachFeet = reachFeet;
            }
        }

        if (weapon is null)
        {
            return null;
        }

        bool wasInReach =
            CalculateDistanceFeet(candidate.Position, fromPosition)
                <= bestReachFeet;
        bool staysInReach =
            CalculateDistanceFeet(candidate.Position, toPosition)
                <= bestReachFeet;

        if (!wasInReach || staysInReach)
        {
            return null;
        }

        return new EncounterOpportunityAttack
        {
            AttackerCombatantId = candidate.Combatant.CombatantId,
            WeaponId = weapon.WeaponId,
            FromPosition = fromPosition
        };
    }

    /// Chebyshev distance, matching how every other reach and range check in
    /// this engine measures the grid (see the weapon-attack prerequisite
    /// rules' own copy) — a diagonal square is one square, not one and a
    /// half.
    private static int CalculateDistanceFeet(
        GridPosition first,
        GridPosition second)
    {
        int horizontalSquares = Math.Abs(first.X - second.X);
        int verticalSquares = Math.Abs(first.Y - second.Y);

        return checked(
            Math.Max(horizontalSquares, verticalSquares) * FeetPerGridSquare);
    }
}
