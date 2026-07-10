using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Definitions;

public sealed record DamageResponseDefinition
{
    public required string DamageType { get; init; }

    public required DamageResponseType ResponseType { get; init; }
}