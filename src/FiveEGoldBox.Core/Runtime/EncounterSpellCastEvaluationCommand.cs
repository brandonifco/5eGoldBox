namespace FiveEGoldBox.Core.Runtime;

internal sealed record EncounterSpellCastEvaluationCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string SpellId { get; init; }

    public int? FirstAttackRoll { get; init; }

    public int? SecondAttackRoll { get; init; }

    public IReadOnlyList<int> AttackContributionRolls { get; init; }
        = Array.Empty<int>();

    public int? SavingThrowRoll { get; init; }

    public IReadOnlyList<int> SavingThrowContributionRolls { get; init; }
        = Array.Empty<int>();
}
