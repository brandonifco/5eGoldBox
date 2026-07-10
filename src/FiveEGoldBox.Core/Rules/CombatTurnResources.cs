namespace FiveEGoldBox.Core.Rules;

public sealed record CombatTurnResources
{
    public required bool HasActionAvailable { get; init; }

    public required bool HasBonusActionAvailable { get; init; }

    public required bool HasReactionAvailable { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required int MovementSpentFeet { get; init; }

    public int MovementRemainingFeet => MovementSpeedFeet - MovementSpentFeet;
}