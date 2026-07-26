namespace FiveEGoldBox.Core.Definitions;

/// Something that sits on a creature for a while and changes what it rolls.
///
/// Named separately from the spell that applies it, because what installs an
/// effect and what the effect does are different questions — a potion, a
/// class feature and a monster's aura could all install this one.
public sealed record EffectDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<RollContributionDefinition> Contributions { get; init; }
        = Array.Empty<RollContributionDefinition>();
}
