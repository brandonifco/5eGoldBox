using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record SubraceDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<AbilityScoreIncrease> AbilityScoreIncreases { get; init; }
        = Array.Empty<AbilityScoreIncrease>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Traits { get; init; }
        = Array.Empty<string>();
}