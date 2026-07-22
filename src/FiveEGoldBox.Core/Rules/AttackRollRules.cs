namespace FiveEGoldBox.Core.Rules;

public static class AttackRollRules
{
    public static AttackRollOutcome ResolveOutcome(
        int naturalRoll,
        int attackBonus,
        int targetArmorClass)
    {
        ValidateNaturalRoll(naturalRoll);

        if (naturalRoll == 1)
        {
            return AttackRollOutcome.Miss;
        }

        if (naturalRoll == 20)
        {
            return AttackRollOutcome.CriticalHit;
        }

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            attackBonus);

        return ResolveOutcomeFromTotal(
            naturalRoll,
            total,
            targetArmorClass);
    }

    public static AttackRollOutcome ResolveOutcome(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int attackBonus,
        int targetArmorClass)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        return ResolveOutcome(
            naturalRoll,
            attackBonus,
            targetArmorClass);
    }

    public static AttackRollResult ResolveResult(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int attackBonus,
        int targetArmorClass)
    {
        int naturalRoll = D20Rules.ResolveNaturalRoll(
            rollMode,
            firstRoll,
            secondRoll);

        int total = D20Rules.ResolveTotal(
            naturalRoll,
            attackBonus);

        AttackRollOutcome outcome = ResolveOutcomeFromTotal(
            naturalRoll,
            total,
            targetArmorClass);

        return new AttackRollResult
        {
            RollMode = rollMode,
            FirstRoll = firstRoll,
            SecondRoll = secondRoll,
            NaturalRoll = naturalRoll,
            AttackBonus = attackBonus,
            Total = total,
            TargetArmorClass = targetArmorClass,
            Outcome = outcome
        };
    }

    private static AttackRollOutcome ResolveOutcomeFromTotal(
        int naturalRoll,
        int total,
        int targetArmorClass)
    {
        if (naturalRoll == 1)
        {
            return AttackRollOutcome.Miss;
        }

        if (naturalRoll == 20)
        {
            return AttackRollOutcome.CriticalHit;
        }

        return total >= targetArmorClass
            ? AttackRollOutcome.Hit
            : AttackRollOutcome.Miss;
    }

    private static void ValidateNaturalRoll(int naturalRoll)
    {
        if (naturalRoll is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(naturalRoll),
                naturalRoll,
                "Natural attack roll must be between 1 and 20.");
        }
    }
}
