using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Internal;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// Asks a combatant what it contributes to a roll, and totals what came back.
///
/// This is the seam the spellcasting design turns on, and it is deliberately
/// blind to where a contribution came from. Bless arrives on an effect sitting
/// on a creature; Sneak Attack is part of what a rogue is and sits on the
/// combat profile. Neither is privileged here, which is the point — a
/// contribution is a contribution.
///
/// Two calls rather than one, deliberately: `Resolve` says what the roll needs
/// before it is rolled, and `Total` consumes what the caller then rolled.
/// Collapsing them would put randomness back inside the rules.
public static class RollContributionRules
{
    /// The same question asked of an encounter rather than a participant, for
    /// callers holding a combatant ID — which is most of them.
    public static RollContributionSet Resolve(
        EncounterState state,
        string combatantId,
        RollContributionTarget target,
        RollContributionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(combatantId))
        {
            throw new ArgumentException(
                "Combatant ID is required.",
                nameof(combatantId));
        }

        EncounterParticipantState participant =
            state.Participants.FirstOrDefault(
                candidate => string.Equals(
                    candidate.Combatant.CombatantId,
                    combatantId,
                    StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"Combatant '{combatantId}' is not an encounter participant.",
                nameof(combatantId));

        return Resolve(
            participant,
            target,
            context);
    }

    public static RollContributionSet Resolve(
        EncounterParticipantState participant,
        RollContributionTarget target,
        RollContributionContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(participant);

        CoreEnumValidation.RequireDefined(
            target,
            nameof(target),
            "Unsupported roll contribution target.");

        int flatBonus = 0;
        List<DieType> requiredDice = [];

        // What the combatant is, then what has happened to it. The order fixes
        // the order of the dice the caller is asked for, and nothing else
        // depends on it.
        Gather(
            participant.CombatProfile.Contributions,
            target,
            context,
            ref flatBonus,
            requiredDice);

        foreach (ActiveEffect effect in participant.ActiveEffects)
        {
            ArgumentNullException.ThrowIfNull(effect);

            // An effect whose rounds have run out is over.
            // EncounterTurnAdvancementRules removes an expired effect at the
            // start of the next round, so this guard only ever catches one
            // applied with no duration left to it in the first place.
            if (effect.RemainingRounds <= 0)
            {
                continue;
            }

            Gather(
                effect.Contributions,
                target,
                context,
                ref flatBonus,
                requiredDice);
        }

        return new RollContributionSet
        {
            Target = target,
            FlatBonus = flatBonus,
            RequiredDice = Array.AsReadOnly(
                requiredDice.ToArray())
        };
    }

    private static void Gather(
        IReadOnlyList<RollContributionDefinition> contributions,
        RollContributionTarget target,
        RollContributionContext? context,
        ref int flatBonus,
        List<DieType> requiredDice)
    {
        foreach (RollContributionDefinition contribution
            in contributions)
        {
            ArgumentNullException.ThrowIfNull(contribution);

            if (contribution.Target != target
                || !Holds(contribution, context))
            {
                continue;
            }

            flatBonus = checked(
                flatBonus + contribution.FlatBonus);

            AppendDice(
                requiredDice,
                contribution.Dice);
        }
    }

    /// Every condition, or none of it. A contribution that asks a question the
    /// roll cannot answer is an error rather than a "no" — silently dropping
    /// Sneak Attack because the damage roll forgot to say how the attack went
    /// is the same class of bug as handing in too few dice.
    private static bool Holds(
        RollContributionDefinition contribution,
        RollContributionContext? context)
    {
        foreach (RollContributionCondition condition
            in contribution.Conditions)
        {
            CoreEnumValidation.RequireDefined(
                condition,
                nameof(contribution),
                "Unsupported roll contribution condition.");

            if (context is null)
            {
                throw new InvalidOperationException(
                    $"A contribution to the {contribution.Target} asks '{condition}', but the roll supplied no context to answer it.");
            }

            if (!Holds(condition, context))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Holds(
        RollContributionCondition condition,
        RollContributionContext context)
    {
        return condition switch
        {
            // Advantage qualifies on its own. Otherwise a crowded target
            // qualifies, so long as nothing is spoiling the shot — and
            // advantage and disadvantage having already cancelled each other
            // is why those two clauses are all this takes.
            RollContributionCondition.AdvantageOrAdjacentEnemy =>
                context.AttackRollMode == D20RollMode.Advantage
                || (context.AttackRollMode != D20RollMode.Disadvantage
                    && context.TargetHasAdjacentEnemy),

            RollContributionCondition.FinesseOrRangedWeapon =>
                context.WeaponIsFinesseOrRanged,

            _ => throw new InvalidOperationException(
                "Validated roll contribution condition was not handled.")
        };
    }

    /// What the contributions came to, given the dice the caller rolled for
    /// them. The count has to match exactly: a caller that rolled the wrong
    /// number of dice did not ask first, and silently ignoring the difference
    /// would make a blessed attack quietly unblessed.
    public static int Total(
        RollContributionSet contributions,
        IReadOnlyList<int> rolls)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        ArgumentNullException.ThrowIfNull(rolls);

        if (rolls.Count
            != contributions.RequiredDice.Count)
        {
            throw new ArgumentException(
                $"Expected {contributions.RequiredDice.Count} contribution roll(s) for the {contributions.Target}, but received {rolls.Count}.",
                nameof(rolls));
        }

        int total = contributions.FlatBonus;

        for (int index = 0;
            index < rolls.Count;
            index++)
        {
            int maximumRoll =
                (int)contributions.RequiredDice[index];

            if (rolls[index] is < 1
                || rolls[index] > maximumRoll)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rolls),
                    rolls[index],
                    $"Contribution roll must be between 1 and {maximumRoll}.");
            }

            total = checked(total + rolls[index]);
        }

        return total;
    }

    private static void AppendDice(
        List<DieType> requiredDice,
        DamageDice? dice)
    {
        if (dice is null)
        {
            return;
        }

        if (dice.Count < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dice),
                dice.Count,
                "A roll contribution's dice count must be at least 1.");
        }

        CoreEnumValidation.RequireDefined(
            dice.Die,
            nameof(dice),
            "Unsupported roll contribution die.");

        for (int index = 0;
            index < dice.Count;
            index++)
        {
            requiredDice.Add(dice.Die);
        }
    }
}
