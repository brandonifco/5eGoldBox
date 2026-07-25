namespace FiveEGoldBox.Application.Combat;

public sealed record CombatDieRoll
{
    internal CombatDieRoll(
        int ordinal,
        int sides,
        int value,
        CombatDiePurpose purpose)
    {
        Ordinal = ordinal;
        Sides = sides;
        Value = value;
        Purpose = purpose;
    }

    /// Position of this roll in the session's deterministic random sequence.
    public int Ordinal { get; }

    public int Sides { get; }

    public int Value { get; }

    public CombatDiePurpose Purpose { get; }
}
