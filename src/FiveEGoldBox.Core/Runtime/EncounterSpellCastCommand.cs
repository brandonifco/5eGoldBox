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

    /// The dice the caster's own effects added to that attack roll, in the
    /// order the prerequisite evaluation asked for them.
    public IReadOnlyList<int> AttackContributionRolls { get; init; }
        = Array.Empty<int>();

    /// The target's saving throw. Required for a spell resisted by one.
    public int? SavingThrowRoll { get; init; }

    /// The dice the target's own effects added to that saving throw. A blessed
    /// creature resists a Sacred Flame a d4 better.
    public IReadOnlyList<int> SavingThrowContributionRolls { get; init; }
        = Array.Empty<int>();

    /// The spell's own dice, already rolled. The caller owns randomness, the
    /// same way it does for a weapon attack.
    public required IReadOnlyList<int> EffectRolls { get; init; }

    /// The target's Constitution saving throw against losing concentration —
    /// unrelated to SavingThrowRoll above, which is the spell's own save.
    /// Required when the target has a ConcentratingOnEffectId and this cast
    /// deals damage that leaves it conscious; ignored otherwise.
    public int? ConcentrationSavingThrowRoll { get; init; }

    /// The dice the target's own effects added to that saving throw.
    public IReadOnlyList<int> ConcentrationSavingThrowContributionRolls
    { get; init; } = Array.Empty<int>();
}
