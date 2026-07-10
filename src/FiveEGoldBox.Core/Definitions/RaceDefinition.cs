using FiveEGoldBox.Core.Rules;
namespace FiveEGoldBox.Core.Definitions;

public sealed record RaceDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public CharacterSize Size { get; init; } = CharacterSize.Medium;

    public required int BaseSpeedFeet { get; init; }

    public IReadOnlyList<AbilityScoreIncrease> AbilityScoreIncreases { get; init; }
        = Array.Empty<AbilityScoreIncrease>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Traits { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<SubraceDefinition> Subraces { get; init; }
        = Array.Empty<SubraceDefinition>();

    public IReadOnlyList<SenseDefinition> Senses { get; init; }
        = Array.Empty<SenseDefinition>();
}