namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDamageCommand
{
    public required long ExpectedRevision { get; init; }

    public required string TargetCombatantId { get; init; }

    public required int DamageAmount { get; init; }

    public required bool IsCriticalHit { get; init; }

    /// The target's Constitution saving throw against losing concentration.
    /// Required when the target has a ConcentratingOnEffectId and this
    /// damage leaves it conscious; ignored otherwise — an incapacitated
    /// target's concentration breaks automatically, no roll needed.
    public int? ConcentrationSavingThrowRoll { get; init; }

    /// The dice the target's own effects added to that saving throw.
    public IReadOnlyList<int> ConcentrationSavingThrowContributionRolls
    { get; init; } = Array.Empty<int>();
}
