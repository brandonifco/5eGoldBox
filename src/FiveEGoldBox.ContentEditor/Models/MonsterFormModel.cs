using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for MonsterDefinition -- see
/// WeaponFormModel's header comment for why this exists.
public sealed class MonsterFormModel
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public int MaximumHitPoints { get; set; }

    public int ArmorClass { get; set; }

    public int MovementSpeedFeet { get; set; }

    public CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; set; } = CombatantZeroHitPointPolicy.Defeated;

    /// Exactly six entries, one per Ability enum value -- a fixed six-row
    /// form, not an add/remove list, per
    /// RulesetValidatorMonsterDefinitions.cs's completeness check.
    public List<MonsterAbilityModifierFormModel> AbilityModifiers { get; set; } =
        Enum.GetValues<Ability>()
            .Select(ability => new MonsterAbilityModifierFormModel { Ability = ability })
            .ToList();

    public int ProficiencyBonus { get; set; }

    public bool IsProficientWithWeapons { get; set; } = true;

    public List<MonsterWeaponFormModel> Weapons { get; set; } = [];

    public static MonsterFormModel FromDefinition(
        MonsterDefinition monster)
    {
        MonsterFormModel model = new()
        {
            Id = monster.Id,
            Name = monster.Name,
            MaximumHitPoints = monster.MaximumHitPoints,
            ArmorClass = monster.ArmorClass,
            MovementSpeedFeet = monster.MovementSpeedFeet,
            ZeroHitPointPolicy = monster.ZeroHitPointPolicy,
            ProficiencyBonus = monster.ProficiencyBonus,
            IsProficientWithWeapons = monster.IsProficientWithWeapons,
            Weapons = monster.Weapons.Select(MonsterWeaponFormModel.FromDefinition).ToList()
        };

        foreach (MonsterAbilityModifier modifier in monster.AbilityModifiers)
        {
            MonsterAbilityModifierFormModel? row = model.AbilityModifiers
                .FirstOrDefault(r => r.Ability == modifier.Ability);

            if (row is not null)
            {
                row.Modifier = modifier.Modifier;
            }
        }

        return model;
    }

    public MonsterDefinition ToDefinition()
    {
        return new MonsterDefinition
        {
            Id = Id,
            Name = Name,
            MaximumHitPoints = MaximumHitPoints,
            ArmorClass = ArmorClass,
            MovementSpeedFeet = MovementSpeedFeet,
            ZeroHitPointPolicy = ZeroHitPointPolicy,
            AbilityModifiers = AbilityModifiers.Select(row => row.ToDefinition()).ToList(),
            ProficiencyBonus = ProficiencyBonus,
            IsProficientWithWeapons = IsProficientWithWeapons,
            Weapons = Weapons.Select(row => row.ToDefinition()).ToList()
        };
    }
}

public sealed class MonsterAbilityModifierFormModel
{
    public Ability Ability { get; set; }

    public int Modifier { get; set; }

    public MonsterAbilityModifier ToDefinition()
    {
        return new MonsterAbilityModifier { Ability = Ability, Modifier = Modifier };
    }
}

public sealed class MonsterWeaponFormModel
{
    public string WeaponId { get; set; } = "";

    /// "" means no ammunition item.
    public string AmmunitionItemId { get; set; } = "";

    public int? AmmunitionQuantity { get; set; }

    public static MonsterWeaponFormModel FromDefinition(
        MonsterWeaponDefinition weapon)
    {
        return new MonsterWeaponFormModel
        {
            WeaponId = weapon.WeaponId,
            AmmunitionItemId = weapon.AmmunitionItemId ?? "",
            AmmunitionQuantity = weapon.AmmunitionQuantity
        };
    }

    public MonsterWeaponDefinition ToDefinition()
    {
        return new MonsterWeaponDefinition
        {
            WeaponId = WeaponId,
            AmmunitionItemId = string.IsNullOrWhiteSpace(AmmunitionItemId) ? null : AmmunitionItemId,
            AmmunitionQuantity = AmmunitionQuantity
        };
    }
}
