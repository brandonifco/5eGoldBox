using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.ContentEditor.Services;

/// The one seam every content-editor page saves and loads through.
///
/// Every write is atomic and validated: re-read the file fresh, build the
/// updated list for the one array being changed, render just that array's
/// new text (RulesetJsonFormatting), splice it into a fresh copy of the
/// file's bytes (RulesetJsonSplicer) written to a temp file, validate the
/// temp file through the same public ContentPackValidation.ValidateRulesetPack
/// facade the standalone `validate` console command uses, and only then
/// replace the real file -- a save that fails validation (including a
/// delete that breaks a cross-reference, like a monster still carrying a
/// weapon being deleted) leaves the real file completely untouched.
public sealed class RulesetContentService
{
    private readonly string _filePath;

    public RulesetContentService()
        : this(RepositoryLocator.ResolveCoreRulesetPackPath())
    {
    }

    internal RulesetContentService(
        string filePath)
    {
        _filePath = filePath;
    }

    /// Exposed only for tests, so a test can assert a rejected write left
    /// the same file it targeted completely untouched.
    internal string FilePathForTesting => _filePath;

    // ----- Weapons -----

    public IReadOnlyList<WeaponDefinition> LoadWeapons()
    {
        return RulesetPackDocument.Read(_filePath).Weapons;
    }

    public WeaponDefinition? FindWeapon(
        string id)
    {
        return LoadWeapons().FirstOrDefault(weapon => weapon.Id == id);
    }

    public ValidationResult SaveWeapon(
        WeaponDefinition weapon)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<WeaponDefinition> updated = Upsert(document.Weapons, weapon, w => w.Id);
        return WriteAndValidate("Weapons", RulesetJsonFormatting.RenderWeapons(updated));
    }

    public ValidationResult DeleteWeapon(
        string id)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<WeaponDefinition> updated = document.Weapons
            .Where(weapon => weapon.Id != id)
            .ToList();
        return WriteAndValidate("Weapons", RulesetJsonFormatting.RenderWeapons(updated));
    }

    // ----- Spells -----

    public IReadOnlyList<SpellDefinition> LoadSpells()
    {
        return RulesetPackDocument.Read(_filePath).Spells;
    }

    public SpellDefinition? FindSpell(
        string id)
    {
        return LoadSpells().FirstOrDefault(spell => spell.Id == id);
    }

    public ValidationResult SaveSpell(
        SpellDefinition spell)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<SpellDefinition> updated = Upsert(document.Spells, spell, s => s.Id);
        return WriteAndValidate("Spells", RulesetJsonFormatting.RenderSpells(updated));
    }

    public ValidationResult DeleteSpell(
        string id)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<SpellDefinition> updated = document.Spells
            .Where(spell => spell.Id != id)
            .ToList();
        return WriteAndValidate("Spells", RulesetJsonFormatting.RenderSpells(updated));
    }

    // ----- Equipment items -----

    public IReadOnlyList<EquipmentItemDefinition> LoadEquipmentItems()
    {
        return RulesetPackDocument.Read(_filePath).EquipmentItems;
    }

    public EquipmentItemDefinition? FindEquipmentItem(
        string id)
    {
        return LoadEquipmentItems().FirstOrDefault(item => item.Id == id);
    }

    public ValidationResult SaveEquipmentItem(
        EquipmentItemDefinition item)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<EquipmentItemDefinition> updated = Upsert(document.EquipmentItems, item, i => i.Id);
        return WriteAndValidate("EquipmentItems", RulesetJsonFormatting.RenderEquipmentItems(updated));
    }

    public ValidationResult DeleteEquipmentItem(
        string id)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<EquipmentItemDefinition> updated = document.EquipmentItems
            .Where(item => item.Id != id)
            .ToList();
        return WriteAndValidate("EquipmentItems", RulesetJsonFormatting.RenderEquipmentItems(updated));
    }

    // ----- Monsters -----

    public IReadOnlyList<MonsterDefinition> LoadMonsters()
    {
        return RulesetPackDocument.Read(_filePath).Monsters;
    }

    public MonsterDefinition? FindMonster(
        string id)
    {
        return LoadMonsters().FirstOrDefault(monster => monster.Id == id);
    }

    public ValidationResult SaveMonster(
        MonsterDefinition monster)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<MonsterDefinition> updated = Upsert(document.Monsters, monster, m => m.Id);
        return WriteAndValidate("Monsters", RulesetJsonFormatting.RenderMonsters(updated));
    }

    public ValidationResult DeleteMonster(
        string id)
    {
        RulesetPackDocument document = RulesetPackDocument.Read(_filePath);
        List<MonsterDefinition> updated = document.Monsters
            .Where(monster => monster.Id != id)
            .ToList();
        return WriteAndValidate("Monsters", RulesetJsonFormatting.RenderMonsters(updated));
    }

    // ----- Cross-reference picker sources -----

    /// Read-only from this editor's perspective: EffectDefinition is not one
    /// of the four managed content kinds, just a reference source for
    /// SpellDefinition.AppliedEffectId.
    public IReadOnlyList<EffectDefinition> LoadEffects()
    {
        return RulesetPackDocument.Read(_filePath).Effects;
    }

    /// Reference sources for the campaign roster editor. Not editable here
    /// -- races, classes, backgrounds and skills aren't among the four
    /// managed content kinds -- but a roster entry has to name real ones, and
    /// reading their ids doesn't require being able to author them.
    public IReadOnlyList<RaceDefinition> LoadRaces()
    {
        return RulesetPackDocument.Read(_filePath).Races;
    }

    public IReadOnlyList<ClassDefinition> LoadClasses()
    {
        return RulesetPackDocument.Read(_filePath).Classes;
    }

    public IReadOnlyList<BackgroundDefinition> LoadBackgrounds()
    {
        return RulesetPackDocument.Read(_filePath).Backgrounds;
    }

    public IReadOnlyList<SkillDefinition> LoadSkills()
    {
        return RulesetPackDocument.Read(_filePath).Skills;
    }

    // ----- Shared write path -----

    private static List<T> Upsert<T>(
        List<T> existing,
        T value,
        Func<T, string> idSelector)
    {
        string id = idSelector(value);
        List<T> updated = [.. existing];
        int index = updated.FindIndex(item => idSelector(item) == id);

        if (index >= 0)
        {
            updated[index] = value;
        }
        else
        {
            updated.Add(value);
        }

        return updated;
    }

    private ValidationResult WriteAndValidate(
        string rootPropertyName,
        string replacementArrayText)
    {
        byte[] original = File.ReadAllBytes(_filePath);
        byte[] updated = RulesetJsonSplicer.ReplaceRootPropertyValue(
            original,
            rootPropertyName,
            replacementArrayText);

        string tempPath = _filePath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            File.WriteAllBytes(tempPath, updated);

            ValidationResult validation = ContentPackValidation.ValidateRulesetPack([tempPath]);

            if (validation.IsValid)
            {
                File.Copy(tempPath, _filePath, overwrite: true);
            }

            return validation;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
