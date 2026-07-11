using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Characters;

public sealed record CharacterDamageResponse
{
    public required string DamageType { get; init; }

    public required DamageResponseType ResponseType { get; init; }
}
