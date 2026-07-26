using FiveEGoldBox.Core.Characters;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterCombatProfile
{
    public required int ArmorClass { get; init; }

    public IReadOnlyList<WeaponAttack> WeaponAttacks { get; init; }
        = Array.Empty<WeaponAttack>();

    public IReadOnlyList<SpellAttack> SpellAttacks { get; init; }
        = Array.Empty<SpellAttack>();

    /// What this combatant can spend. Spell slots today; anything with a
    /// finite number of uses later.
    public IReadOnlyList<CombatantResource> Resources { get; init; }
        = Array.Empty<CombatantResource>();

    public IReadOnlyList<SavingThrowBonus> SavingThrowBonuses { get; init; }
        = Array.Empty<SavingThrowBonus>();

    public IReadOnlyList<CharacterDamageResponse> DamageResponses { get; init; }
        = Array.Empty<CharacterDamageResponse>();
}
