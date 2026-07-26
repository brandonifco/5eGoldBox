namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellCastCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }

    /// The caster's attack roll. Required for a spell resolved by one, and
    /// meaningless otherwise.
    public int? FirstAttackRoll { get; init; }

    public int? SecondAttackRoll { get; init; }

    /// The target's saving throw. Required for a spell resisted by one.
    public int? SavingThrowRoll { get; init; }

    /// The spell's own dice, already rolled. The caller owns randomness, the
    /// same way it does for a weapon attack.
    public required IReadOnlyList<int> EffectRolls { get; init; }
}
