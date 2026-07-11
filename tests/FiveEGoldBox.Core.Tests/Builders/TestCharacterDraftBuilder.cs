using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests.Builders;

internal sealed class TestCharacterDraftBuilder
{
    private string? _name = "Test Character";
    private int _level = 1;
    private string? _raceId = "race.human";
    private string? _subraceId;
    private string? _classId = "class.fighter";
    private string? _backgroundId = "background.soldier";
    private AbilityScoreGenerationMethod _abilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray;
    private IReadOnlyDictionary<Ability, int> _baseAbilityScores = StandardArrayScores();
    private IReadOnlyList<string> _selectedSkillIds = Array.Empty<string>();
    private string? _equippedArmorId;
    private string? _equippedShieldId;
    private IReadOnlyList<string> _equippedWeaponIds = Array.Empty<string>();
    private IReadOnlyList<InventoryItemDraft> _inventoryItems = Array.Empty<InventoryItemDraft>();
    private CurrencyAmount _currency = new();

    public static CharacterDraft Valid()
    {
        return new TestCharacterDraftBuilder().Build();
    }

    public TestCharacterDraftBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    public TestCharacterDraftBuilder WithLevel(int level)
    {
        _level = level;
        return this;
    }

    public TestCharacterDraftBuilder WithRaceId(string? raceId)
    {
        _raceId = raceId;
        return this;
    }

    public TestCharacterDraftBuilder WithSubraceId(string? subraceId)
    {
        _subraceId = subraceId;
        return this;
    }

    public TestCharacterDraftBuilder WithClassId(string? classId)
    {
        _classId = classId;
        return this;
    }

    public TestCharacterDraftBuilder WithBackgroundId(string? backgroundId)
    {
        _backgroundId = backgroundId;
        return this;
    }

    public TestCharacterDraftBuilder WithAbilityScoreGenerationMethod(
        AbilityScoreGenerationMethod abilityScoreGenerationMethod)
    {
        _abilityScoreGenerationMethod = abilityScoreGenerationMethod;
        return this;
    }

    public TestCharacterDraftBuilder WithBaseAbilityScores(
        IReadOnlyDictionary<Ability, int> baseAbilityScores)
    {
        _baseAbilityScores = baseAbilityScores;
        return this;
    }

    public TestCharacterDraftBuilder WithSelectedSkillIds(
        IReadOnlyList<string> selectedSkillIds)
    {
        _selectedSkillIds = selectedSkillIds;
        return this;
    }

    public TestCharacterDraftBuilder WithEquippedArmorId(string? equippedArmorId)
    {
        _equippedArmorId = equippedArmorId;
        return this;
    }

    public TestCharacterDraftBuilder WithEquippedShieldId(string? equippedShieldId)
    {
        _equippedShieldId = equippedShieldId;
        return this;
    }

    public TestCharacterDraftBuilder WithEquippedWeaponIds(
        IReadOnlyList<string> equippedWeaponIds)
    {
        _equippedWeaponIds = equippedWeaponIds;
        return this;
    }

    public TestCharacterDraftBuilder WithInventoryItems(
        IReadOnlyList<InventoryItemDraft> inventoryItems)
    {
        _inventoryItems = inventoryItems;
        return this;
    }

    public TestCharacterDraftBuilder WithCurrency(CurrencyAmount currency)
    {
        _currency = currency;
        return this;
    }

    public CharacterDraft Build()
    {
        return new CharacterDraft
        {
            Name = _name,
            Level = _level,
            RaceId = _raceId,
            SubraceId = _subraceId,
            ClassId = _classId,
            BackgroundId = _backgroundId,
            AbilityScoreGenerationMethod = _abilityScoreGenerationMethod,
            BaseAbilityScores = _baseAbilityScores,
            SelectedSkillIds = _selectedSkillIds,
            EquippedArmorId = _equippedArmorId,
            EquippedShieldId = _equippedShieldId,
            EquippedWeaponIds = _equippedWeaponIds,
            InventoryItems = _inventoryItems,
            Currency = _currency
        };
    }

    public static IReadOnlyDictionary<Ability, int> StandardArrayScores()
    {
        return new Dictionary<Ability, int>
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };
    }
}
