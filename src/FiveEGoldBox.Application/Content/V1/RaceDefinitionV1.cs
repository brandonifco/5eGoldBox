namespace FiveEGoldBox.Application.Content.V1;

internal sealed record RaceDefinitionV1
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public CharacterSizeV1 Size { get; init; } = CharacterSizeV1.Medium;

    public required int BaseSpeedFeet { get; init; }

    public IReadOnlyList<AbilityScoreIncreaseV1> AbilityScoreIncreases { get; init; }
        = Array.Empty<AbilityScoreIncreaseV1>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Traits { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<SubraceDefinitionV1> Subraces { get; init; }
        = Array.Empty<SubraceDefinitionV1>();

    public IReadOnlyList<SenseDefinitionV1> Senses { get; init; }
        = Array.Empty<SenseDefinitionV1>();

    public IReadOnlyList<MovementSpeedDefinitionV1> MovementSpeeds { get; init; }
        = Array.Empty<MovementSpeedDefinitionV1>();

    public IReadOnlyList<DamageResponseDefinitionV1> DamageResponses { get; init; }
        = Array.Empty<DamageResponseDefinitionV1>();

    public IReadOnlyList<ConditionImmunityDefinitionV1> ConditionImmunities { get; init; }
        = Array.Empty<ConditionImmunityDefinitionV1>();
}
