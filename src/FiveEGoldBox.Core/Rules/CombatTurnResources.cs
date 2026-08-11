namespace FiveEGoldBox.Core.Rules;

public sealed record CombatTurnResources
{
    public required bool HasActionAvailable { get; init; }

    public required bool HasBonusActionAvailable { get; init; }

    public required bool HasReactionAvailable { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required int MovementSpentFeet { get; init; }

    /// Set by taking the Disengage action, cleared by StartTurn — so it
    /// lasts exactly the rest of the turn it was taken on, which is what
    /// 5e's own wording ("your movement doesn't provoke opportunity
    /// attacks for the rest of the turn") describes.
    ///
    /// Not `required`, unlike every other member here: this record is
    /// constructed by hand in a great many tests that predate Disengage
    /// and have nothing to do with it, and defaulting to "has not
    /// disengaged" is both the correct default and the one that leaves
    /// them saying what they already said.
    public bool HasDisengaged { get; init; }

    public int MovementRemainingFeet => MovementSpeedFeet - MovementSpentFeet;
}
