namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterSpellCastCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    /// Everyone else the spell reaches. Bless takes up to three; a spell that
    /// takes one leaves this empty. The first target is named separately
    /// because every spell has at least one.
    public IReadOnlyList<string> AdditionalTargetCombatantIds { get; init; }
        = Array.Empty<string>();

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
