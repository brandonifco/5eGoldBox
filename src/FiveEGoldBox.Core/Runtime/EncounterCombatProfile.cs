using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterCombatProfile
{
    public required int ArmorClass { get; init; }

    /// What this combatant contributes to rolls by being what it is — class
    /// features, chiefly. Separate from the effects sitting on it, because a
    /// feature is not something that happened to you and does not expire.
    public IReadOnlyList<RollContributionDefinition> Contributions
    { get; init; } = Array.Empty<RollContributionDefinition>();

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
