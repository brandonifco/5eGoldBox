using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// Asks a combatant what its effects contribute to a roll, and totals what
/// came back.
///
/// This is the seam the spellcasting design turns on. Bless installs a
/// contribution; so will Rage, Sneak Attack and a fighting style. Nothing here
/// knows what a spell is — a contribution arrives on an effect already
/// resolved, and this reads it.
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
        RollContributionTarget target)
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
            target);
    }

    public static RollContributionSet Resolve(
        EncounterParticipantState participant,
        RollContributionTarget target)
    {
        ArgumentNullException.ThrowIfNull(participant);

        if (!Enum.IsDefined(target))
        {
            throw new ArgumentOutOfRangeException(
                nameof(target),
                target,
                "Unsupported roll contribution target.");
        }

        int flatBonus = 0;
        List<DieType> requiredDice = [];

        foreach (ActiveEffect effect in participant.ActiveEffects)
        {
            ArgumentNullException.ThrowIfNull(effect);

            // An effect whose rounds have run out is over. Nothing counts
            // rounds down yet, so this catches only an effect applied with no
            // duration left to it.
            if (effect.RemainingRounds <= 0)
            {
                continue;
            }

            foreach (RollContributionDefinition contribution
                in effect.Contributions)
            {
                ArgumentNullException.ThrowIfNull(contribution);

                if (contribution.Target != target)
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

        return new RollContributionSet
        {
            Target = target,
            FlatBonus = flatBonus,
            RequiredDice = Array.AsReadOnly(
                requiredDice.ToArray())
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

        if (!Enum.IsDefined(dice.Die))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dice),
                dice.Die,
                "Unsupported roll contribution die.");
        }

        for (int index = 0;
            index < dice.Count;
            index++)
        {
            requiredDice.Add(dice.Die);
        }
    }
}
