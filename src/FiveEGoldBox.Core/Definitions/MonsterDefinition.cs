using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Definitions;

/// A bestiary entry — a monster's stats, declared once in the ruleset and
/// referenced by ID from an encounter, the same way a WeaponDefinition is
/// declared once and referenced by ID from whoever carries it.
public sealed record MonsterDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required int MaximumHitPoints { get; init; }

    public required int ArmorClass { get; init; }

    public required int MovementSpeedFeet { get; init; }

    public required CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; init; }

    /// Every ability's modifier. All six are required, because saving throws
    /// need all six and a missing one would only surface when something asked
    /// this monster to make that save.
    public required IReadOnlyList<MonsterAbilityModifier> AbilityModifiers { get; init; }

    public required int ProficiencyBonus { get; init; }

    /// Experience the party earns for defeating this monster, awarded on a
    /// completed encounter's winning side. A CR-derived value, not computed
    /// from anything else on this record -- HP/AC/weapons say how dangerous a
    /// fight is, not how much it should be worth.
    public required int ExperienceValue { get; init; }

    /// Whether this monster is proficient with the weapons it carries. Stat
    /// blocks are proficient with their own gear as a rule, so this defaults
    /// accordingly.
    public bool IsProficientWithWeapons { get; init; } = true;

    public required IReadOnlyList<MonsterWeaponDefinition> Weapons { get; init; }
}
