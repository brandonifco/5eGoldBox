using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record SubraceDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    /// Player-facing prose for a character-creation screen, so a choice is
    /// more than a bare name to someone who does not already know 5e.
    /// Optional: content that predates this loads unchanged, and a validator
    /// reports what is missing rather than refusing to load it.
    public string? Description { get; init; }

    public IReadOnlyList<AbilityScoreIncrease> AbilityScoreIncreases { get; init; }
        = Array.Empty<AbilityScoreIncrease>();

    public IReadOnlyList<string> Languages { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> Traits { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<SenseDefinition> Senses { get; init; }
        = Array.Empty<SenseDefinition>();

    public IReadOnlyList<MovementSpeedDefinition> MovementSpeeds { get; init; }
        = Array.Empty<MovementSpeedDefinition>();

    public IReadOnlyList<DamageResponseDefinition> DamageResponses { get; init; }
        = Array.Empty<DamageResponseDefinition>();

    public IReadOnlyList<ConditionImmunityDefinition> ConditionImmunities { get; init; }
        = Array.Empty<ConditionImmunityDefinition>();

}
