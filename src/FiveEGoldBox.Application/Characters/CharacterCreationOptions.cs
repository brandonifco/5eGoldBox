using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Characters;

/// Everything a client needs to offer a player the choices
/// CharacterCreationRules will then validate -- the read half of character
/// creation, beside Validate/CreateParty's write half.
///
/// Deliberately carries the Core definitions themselves rather than a
/// parallel set of view models, unlike CombatView or ExplorationMapView.
/// Those exist because the definitions behind them are internal, so a
/// projection was the only way to cross the boundary at all; every type
/// named here is already public in Core.Definitions, so a second hierarchy
/// mirroring it would be duplication to keep in sync, not encapsulation.
/// What this record does add is scope: a character creator has no business
/// reading the bestiary, so RulesetDefinition is not simply handed over
/// whole.
///
/// Each list preserves the ruleset's own authored order rather than sorting,
/// so a client renders choices in the order content declares them.
public sealed record CharacterCreationOptions
{
    public required IReadOnlyList<RaceDefinition> Races { get; init; }

    public required IReadOnlyList<ClassDefinition> Classes { get; init; }

    public required IReadOnlyList<BackgroundDefinition> Backgrounds { get; init; }

    /// Every skill the ruleset declares. Which of them a given character may
    /// actually choose comes from its own class (ClassDefinition.SkillChoices
    /// and NumberOfSkillChoices); a background contributes its
    /// SkillProficiencies on top of that without being chosen.
    public required IReadOnlyList<SkillDefinition> Skills { get; init; }

    public required IReadOnlyList<WeaponDefinition> Weapons { get; init; }

    /// Only a class with a SpellcastingAbility may prepare any of these.
    public required IReadOnlyList<SpellDefinition> Spells { get; init; }

    public required IReadOnlyList<EquipmentItemDefinition> EquipmentItems { get; init; }
}
