using FiveEGoldBox.Core.Characters;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterCombatProfile
{
    public required int ArmorClass { get; init; }

    public IReadOnlyList<WeaponAttack> WeaponAttacks { get; init; }
        = Array.Empty<WeaponAttack>();

    public IReadOnlyList<CharacterDamageResponse> DamageResponses { get; init; }
        = Array.Empty<CharacterDamageResponse>();
}
