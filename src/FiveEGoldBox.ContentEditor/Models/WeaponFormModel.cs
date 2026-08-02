using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for WeaponDefinition, needed purely
/// because Blazor's <EditForm> binding needs { get; set; } properties and
/// doesn't work cleanly against WeaponDefinition's init-only record
/// properties. Converts to/from the real WeaponDefinition only at the
/// load/submit boundary (FromDefinition/ToDefinition below); everything
/// else in the editor (reading, saving, validating) works with the real
/// Core type.
public sealed class WeaponFormModel
{
    /// The fixed set of weapon properties RulesetValidatorWeaponDefinitions
    /// accepts (FiveEGoldBox.Core.Rules.RuleIds.WeaponProperties) -- a
    /// checkbox group from this fixed set, not free text.
    public static readonly IReadOnlyList<string> KnownProperties =
    [
        RuleIds.WeaponProperties.Ammunition,
        RuleIds.WeaponProperties.Finesse,
        RuleIds.WeaponProperties.Heavy,
        RuleIds.WeaponProperties.TwoHanded,
        RuleIds.WeaponProperties.Versatile
    ];

    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public WeaponCategory Category { get; set; } = WeaponCategory.Simple;

    public WeaponAttackKind AttackKind { get; set; } = WeaponAttackKind.Melee;

    public int DamageCount { get; set; } = 1;

    public DieType DamageDie { get; set; } = DieType.D6;

    public bool HasVersatileDamage { get; set; }

    public int VersatileDamageCount { get; set; } = 1;

    public DieType VersatileDamageDie { get; set; } = DieType.D6;

    public string DamageType { get; set; } = "";

    public Dictionary<string, bool> Properties { get; set; } =
        KnownProperties.ToDictionary(property => property, _ => false);

    public int? ReachFeet { get; set; }

    public int? NormalRangeFeet { get; set; }

    public int? LongRangeFeet { get; set; }

    /// "" means no ammunition item (the None option in the picker).
    public string AmmunitionItemId { get; set; } = "";

    public decimal WeightPounds { get; set; }

    public int? CostInCopperPieces { get; set; }

    public static WeaponFormModel FromDefinition(
        WeaponDefinition weapon)
    {
        WeaponFormModel model = new()
        {
            Id = weapon.Id,
            Name = weapon.Name,
            Category = weapon.Category,
            AttackKind = weapon.AttackKind,
            DamageCount = weapon.Damage.Count,
            DamageDie = weapon.Damage.Die,
            HasVersatileDamage = weapon.VersatileDamage is not null,
            VersatileDamageCount = weapon.VersatileDamage?.Count ?? 1,
            VersatileDamageDie = weapon.VersatileDamage?.Die ?? DieType.D6,
            DamageType = weapon.DamageType,
            ReachFeet = weapon.ReachFeet,
            NormalRangeFeet = weapon.NormalRangeFeet,
            LongRangeFeet = weapon.LongRangeFeet,
            AmmunitionItemId = weapon.AmmunitionItemId ?? "",
            WeightPounds = weapon.WeightPounds,
            CostInCopperPieces = weapon.CostInCopperPieces
        };

        foreach (string property in weapon.Properties)
        {
            model.Properties[property] = true;
        }

        return model;
    }

    public WeaponDefinition ToDefinition()
    {
        return new WeaponDefinition
        {
            Id = Id,
            Name = Name,
            Category = Category,
            AttackKind = AttackKind,
            Damage = new DamageDice { Count = DamageCount, Die = DamageDie },
            VersatileDamage = HasVersatileDamage
                ? new DamageDice { Count = VersatileDamageCount, Die = VersatileDamageDie }
                : null,
            DamageType = DamageType,
            Properties = KnownProperties.Where(property => Properties.GetValueOrDefault(property)).ToList(),
            ReachFeet = ReachFeet,
            NormalRangeFeet = NormalRangeFeet,
            LongRangeFeet = LongRangeFeet,
            AmmunitionItemId = string.IsNullOrWhiteSpace(AmmunitionItemId) ? null : AmmunitionItemId,
            WeightPounds = WeightPounds,
            CostInCopperPieces = CostInCopperPieces
        };
    }
}
