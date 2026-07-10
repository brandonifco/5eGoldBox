namespace FiveEGoldBox.Core.Rules;

public static class CombatTurnResourceRules
{
    public static CombatTurnResources StartTurn(
        int movementSpeedFeet)
    {
        if (movementSpeedFeet < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementSpeedFeet),
                movementSpeedFeet,
                "Movement speed must not be negative.");
        }

        return new CombatTurnResources
        {
            HasActionAvailable = true,
            HasBonusActionAvailable = true,
            HasReactionAvailable = true,
            MovementSpeedFeet = movementSpeedFeet,
            MovementSpentFeet = 0
        };
    }

    public static CombatTurnResources SpendAction(
        CombatTurnResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        if (!resources.HasActionAvailable)
        {
            throw new InvalidOperationException(
                "Action has already been spent.");
        }

        return resources with
        {
            HasActionAvailable = false
        };
    }

    public static CombatTurnResources SpendBonusAction(
        CombatTurnResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        if (!resources.HasBonusActionAvailable)
        {
            throw new InvalidOperationException(
                "Bonus action has already been spent.");
        }

        return resources with
        {
            HasBonusActionAvailable = false
        };
    }

    public static CombatTurnResources SpendReaction(
        CombatTurnResources resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        if (!resources.HasReactionAvailable)
        {
            throw new InvalidOperationException(
                "Reaction has already been spent.");
        }

        return resources with
        {
            HasReactionAvailable = false
        };
    }

    public static CombatTurnResources SpendMovement(
        CombatTurnResources resources,
        int movementFeet)
    {
        ArgumentNullException.ThrowIfNull(resources);

        ValidateResources(resources);

        if (movementFeet <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(movementFeet),
                movementFeet,
                "Movement spent must be greater than 0.");
        }

        if (movementFeet > resources.MovementRemainingFeet)
        {
            throw new InvalidOperationException(
                "Movement spent exceeds remaining movement.");
        }

        return resources with
        {
            MovementSpentFeet = resources.MovementSpentFeet + movementFeet
        };
    }

    private static void ValidateResources(
        CombatTurnResources resources)
    {
        if (resources.MovementSpeedFeet < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resources),
                resources.MovementSpeedFeet,
                "Movement speed must not be negative.");
        }

        if (resources.MovementSpentFeet < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resources),
                resources.MovementSpentFeet,
                "Movement spent must not be negative.");
        }

        if (resources.MovementSpentFeet > resources.MovementSpeedFeet)
        {
            throw new ArgumentOutOfRangeException(
                nameof(resources),
                resources.MovementSpentFeet,
                "Movement spent must not exceed movement speed.");
        }
    }
}