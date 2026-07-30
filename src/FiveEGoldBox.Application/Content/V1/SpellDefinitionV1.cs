namespace FiveEGoldBox.Application.Content.V1;

internal sealed record SpellDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required SpellCostKindV1 Cost { get; init; }

    public required int Level { get; init; }

    public required SpellCastingTimeV1 CastingTime { get; init; }

    public required SpellRangeKindV1 RangeKind { get; init; }

    public int? RangeFeet { get; init; }

    public int MaximumTargets { get; init; } = 1;

    public required SpellTargetDispositionV1 Targets { get; init; }

    public required SpellResolutionKindV1 Resolution { get; init; }

    public AbilityV1? SaveAbility { get; init; }

    public SpellSaveOutcomeV1 SaveOutcome { get; init; }
        = SpellSaveOutcomeV1.Negates;

    public IReadOnlyList<SpellEffectDefinitionV1> Effects { get; init; }
        = Array.Empty<SpellEffectDefinitionV1>();

    public string? AppliedEffectId { get; init; }

    public bool RequiresConcentration { get; init; }

    public int? DurationRounds { get; init; }
}
