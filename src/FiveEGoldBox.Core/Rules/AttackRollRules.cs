namespace FiveEGoldBox.Core.Rules;

public static class AttackRollRules
{
    public static AttackRollOutcome ResolveOutcome(
        int naturalRoll,
        int attackBonus,
        int targetArmorClass)
    {
        if (naturalRoll is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(
                nameof(naturalRoll),
                naturalRoll,
                "Natural attack roll must be between 1 and 20.");
        }

        if (naturalRoll == 1)
        {
            return AttackRollOutcome.Miss;
        }

        if (naturalRoll == 20)
        {
            return AttackRollOutcome.CriticalHit;
        }

        int total = naturalRoll + attackBonus;

        return total >= targetArmorClass
            ? AttackRollOutcome.Hit
            : AttackRollOutcome.Miss;
    }
}