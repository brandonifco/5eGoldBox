using System.Globalization;
using FiveEGoldBox.Core.Definitions;
using static FiveEGoldBox.ContentEditor.Services.JsonLayoutPrimitives;

namespace FiveEGoldBox.ContentEditor.Services;

/// Hand-rolled JSON layout for exactly the four content shapes this editor
/// writes (WeaponDefinition, SpellDefinition, EquipmentItemDefinition,
/// MonsterDefinition, and their nested sub-types), matching
/// data/rulesets/campaign/core.json's existing hand-authored formatting
/// byte-for-byte.
///
/// core.json was never produced by a generic serializer -- it uses 4-space
/// indentation, but also a selective single-line compaction style (a nested
/// object with only scalar properties, like DamageDice, is written
/// `{ "Count": 1, "Die": "D8" }` on one line; a top-level content record,
/// like a whole WeaponDefinition, is always fully exploded, one property per
/// line, even when every one of its properties happens to be scalar; an
/// array with 0 or 1 items stays on one line when its item is itself
/// compact, but an array with 2+ items always explodes to one item per
/// line) plus minimal string escaping (a literal apostrophe in "Miller's
/// thrall", not System.Text.Json's default '). No combination of
/// JsonSerializerOptions reproduces this, so RulesetContentService's write
/// path does not round-trip the whole document through
/// JsonNode/JsonSerializer at all -- it locates the exact byte span of the
/// one top-level array being changed (via RulesetJsonSplicer) and replaces
/// only that span with text this class renders, leaving every other byte of
/// the file untouched. This class's own job is narrower than "format
/// arbitrary JSON": it only has to reproduce the layout of the four shapes
/// it actually writes, verified against every existing instance of them in
/// core.json (see NoOpSaveFormattingTests). The shape-agnostic rendering
/// rules themselves live in JsonLayoutPrimitives, shared with
/// ScenarioJsonFormatting.
internal static class RulesetJsonFormatting
{
    // ----- Top-level arrays -----

    internal static string RenderWeapons(IReadOnlyList<WeaponDefinition> weapons)
    {
        return RenderTopLevelArray(weapons, RenderWeapon);
    }

    internal static string RenderSpells(IReadOnlyList<SpellDefinition> spells)
    {
        return RenderTopLevelArray(spells, RenderSpell);
    }

    internal static string RenderEquipmentItems(IReadOnlyList<EquipmentItemDefinition> items)
    {
        return RenderTopLevelArray(items, RenderEquipmentItem);
    }

    internal static string RenderMonsters(IReadOnlyList<MonsterDefinition> monsters)
    {
        return RenderTopLevelArray(monsters, RenderMonster);
    }

    // ----- WeaponDefinition -----

    private static string RenderWeapon(
        WeaponDefinition weapon,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Id", JsonString(weapon.Id)),
            ("Name", JsonString(weapon.Name)),
            ("Category", JsonString(weapon.Category.ToString())),
            ("AttackKind", JsonString(weapon.AttackKind.ToString())),
            ("Damage", RenderDamageDice(weapon.Damage)),
            ("VersatileDamage", weapon.VersatileDamage is { } versatile ? RenderDamageDice(versatile) : null),
            ("DamageType", JsonString(weapon.DamageType)),
            ("Properties", RenderCompactArray(weapon.Properties, column + 8, JsonString)),
            ("ReachFeet", RenderNullableInt(weapon.ReachFeet)),
            ("NormalRangeFeet", RenderNullableInt(weapon.NormalRangeFeet)),
            ("LongRangeFeet", RenderNullableInt(weapon.LongRangeFeet)),
            ("AmmunitionItemId", weapon.AmmunitionItemId is { } ammo ? JsonString(ammo) : null),
            ("WeightPounds", weapon.WeightPounds != 0m ? RenderDecimal(weapon.WeightPounds) : null),
            ("CostInCopperPieces", RenderNullableInt(weapon.CostInCopperPieces))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- SpellDefinition -----

    private static string RenderSpell(
        SpellDefinition spell,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Id", JsonString(spell.Id)),
            ("Name", JsonString(spell.Name)),
            ("Cost", JsonString(spell.Cost.ToString())),
            ("Level", spell.Level.ToString(CultureInfo.InvariantCulture)),
            ("CastingTime", JsonString(spell.CastingTime.ToString())),
            ("RangeKind", JsonString(spell.RangeKind.ToString())),
            ("RangeFeet", RenderNullableInt(spell.RangeFeet)),
            ("MaximumTargets", spell.MaximumTargets != 1 ? spell.MaximumTargets.ToString(CultureInfo.InvariantCulture) : null),
            ("Targets", JsonString(spell.Targets.ToString())),
            ("Resolution", JsonString(spell.Resolution.ToString())),
            ("SaveAbility", spell.SaveAbility is { } saveAbility ? JsonString(saveAbility.ToString()) : null),
            ("SaveOutcome", spell.SaveAbility is not null ? JsonString(spell.SaveOutcome.ToString()) : null),
            ("Effects", RenderAlwaysExplodedArray(spell.Effects, column + 8, RenderSpellEffect)),
            ("AppliedEffectId", spell.AppliedEffectId is { } effectId ? JsonString(effectId) : null),
            ("RequiresConcentration", spell.RequiresConcentration ? "true" : null),
            ("DurationRounds", RenderNullableInt(spell.DurationRounds))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderSpellEffect(
        SpellEffectDefinition effect,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Kind", JsonString(effect.Kind.ToString())),
            ("Dice", RenderDamageDice(effect.Dice)),
            ("Instances", effect.Instances != 1 ? effect.Instances.ToString(CultureInfo.InvariantCulture) : null),
            ("AddsSpellcastingModifier", effect.AddsSpellcastingModifier ? "true" : null),
            ("DamageType", effect.DamageType is { } damageType ? JsonString(damageType) : null)
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- EquipmentItemDefinition -----

    private static string RenderEquipmentItem(
        EquipmentItemDefinition item,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Id", JsonString(item.Id)),
            ("Name", JsonString(item.Name)),
            ("WeightPounds", item.WeightPounds != 0m ? RenderDecimal(item.WeightPounds) : null),
            ("CostInCopperPieces", RenderNullableInt(item.CostInCopperPieces)),
            ("Tags", RenderCompactArray(item.Tags, column + 8, JsonString))
        ];

        return RenderExplodedObject(fields, column);
    }

    // ----- MonsterDefinition -----

    private static string RenderMonster(
        MonsterDefinition monster,
        int column)
    {
        List<(string Key, string? Value)> fields =
        [
            ("Id", JsonString(monster.Id)),
            ("Name", JsonString(monster.Name)),
            ("MaximumHitPoints", monster.MaximumHitPoints.ToString(CultureInfo.InvariantCulture)),
            ("ArmorClass", monster.ArmorClass.ToString(CultureInfo.InvariantCulture)),
            ("MovementSpeedFeet", monster.MovementSpeedFeet.ToString(CultureInfo.InvariantCulture)),
            ("ZeroHitPointPolicy", JsonString(monster.ZeroHitPointPolicy.ToString())),
            ("AbilityModifiers", RenderCompactArrayRequired(monster.AbilityModifiers, column + 8, RenderAbilityModifier)),
            ("ProficiencyBonus", monster.ProficiencyBonus.ToString(CultureInfo.InvariantCulture)),
            ("IsProficientWithWeapons", !monster.IsProficientWithWeapons ? "false" : null),
            ("Weapons", RenderCompactArrayRequired(monster.Weapons, column + 8, RenderMonsterWeapon))
        ];

        return RenderExplodedObject(fields, column);
    }

    private static string RenderAbilityModifier(MonsterAbilityModifier modifier)
    {
        return $$"""{ "Ability": {{JsonString(modifier.Ability.ToString())}}, "Modifier": {{modifier.Modifier.ToString(CultureInfo.InvariantCulture)}} }""";
    }

    private static string RenderMonsterWeapon(MonsterWeaponDefinition weapon)
    {
        List<(string Key, string Value)> fields = [("WeaponId", JsonString(weapon.WeaponId))];

        if (weapon.AmmunitionItemId is { } ammoId)
        {
            fields.Add(("AmmunitionItemId", JsonString(ammoId)));
        }

        if (weapon.AmmunitionQuantity is { } quantity)
        {
            fields.Add(("AmmunitionQuantity", quantity.ToString(CultureInfo.InvariantCulture)));
        }

        return RenderCompactObject(fields);
    }

    // ----- Shared primitives specific to ruleset shapes -----

    private static string RenderDamageDice(
        DamageDice dice)
    {
        return $$"""{ "Count": {{dice.Count.ToString(CultureInfo.InvariantCulture)}}, "Die": {{JsonString(dice.Die.ToString())}} }""";
    }
}
