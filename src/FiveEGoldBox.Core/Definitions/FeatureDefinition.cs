namespace FiveEGoldBox.Core.Definitions;

/// Something a character has by being what it is.
///
/// The same fields as an EffectDefinition today, and deliberately not the same
/// type. An effect is applied, sustained and ends; a feature is simply true of
/// whoever has it, and the two will diverge the moment a feature grants a
/// resource or an action — the other two legs the design says a feature can
/// have, neither of which anything reads yet.
///
/// `ClassDefinition.FeaturesByLevel` names these. Those IDs sat inert from the
/// beginning; Sneak Attack is what they now point at.
public sealed record FeatureDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<RollContributionDefinition> Contributions { get; init; }
        = Array.Empty<RollContributionDefinition>();
}
