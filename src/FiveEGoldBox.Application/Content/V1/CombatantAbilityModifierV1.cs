namespace FiveEGoldBox.Application.Content.V1;

internal sealed record CombatantAbilityModifierV1
{
    public required AbilityV1 Ability { get; init; }

    public required int Modifier { get; init; }
}
