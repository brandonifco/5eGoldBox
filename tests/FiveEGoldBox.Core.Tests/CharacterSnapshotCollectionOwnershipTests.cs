using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterSnapshotCollectionOwnershipTests
{
    [Fact]
    public void Resolve_ProtectsEverySnapshotCollectionAndNestedCollection()
    {
        RichCharacterFixture fixture = CreateRichCharacterFixture();
        CharacterResolver resolver = new(fixture.Ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(fixture.Draft);

        AssertProtectedList(snapshot.MovementSpeeds);
        AssertProtectedList(snapshot.Senses);
        AssertProtectedList(snapshot.DamageResponses);
        AssertProtectedList(snapshot.ConditionImmunities);
        AssertProtectedList(snapshot.InventoryItems);
        AssertProtectedList(snapshot.EquippedWeaponIds);
        AssertProtectedList(snapshot.EquippedWeaponNames);
        AssertProtectedList(snapshot.WeaponAttacks);
        AssertProtectedDictionary(snapshot.AbilityScores, Ability.Strength, 1);
        AssertProtectedDictionary(snapshot.AbilityModifiers, Ability.Strength, 99);
        AssertProtectedList(snapshot.SavingThrowBonuses);
        AssertProtectedList(snapshot.SavingThrowProficiencies);
        AssertProtectedList(snapshot.ArmorProficiencies);
        AssertProtectedList(snapshot.WeaponProficiencies);
        AssertProtectedList(snapshot.ToolProficiencies);
        AssertProtectedList(snapshot.SkillProficiencies);
        AssertProtectedList(snapshot.SkillBonuses);
        AssertProtectedList(snapshot.Languages);
        AssertProtectedList(snapshot.Traits);
        AssertProtectedList(snapshot.ClassFeatures);

        InventoryItemSnapshot inventoryItem = Assert.Single(snapshot.InventoryItems);
        AssertProtectedList(inventoryItem.Tags);

        WeaponAttack weaponAttack = Assert.Single(snapshot.WeaponAttacks);
        AssertProtectedList(weaponAttack.DisadvantageReasons);
        AssertProtectedList(weaponAttack.Properties);

        Assert.Equal(16, snapshot.AbilityScores[Ability.Strength]);
        Assert.Equal(3, snapshot.AbilityModifiers[Ability.Strength]);
        Assert.Equal(2, snapshot.InitiativeBonus);
        Assert.Equal(12, snapshot.ArmorClass);
        Assert.Equal(11, snapshot.MaxHitPoints);
        Assert.Equal(12, snapshot.PassivePerception);
        Assert.Equal(5, weaponAttack.AttackBonus);
        Assert.Equal(3, weaponAttack.DamageBonus);
    }

    [Fact]
    public void Resolve_SourceDraftMutationDoesNotAlterSnapshot()
    {
        RichCharacterFixture fixture = CreateRichCharacterFixture();
        CharacterResolver resolver = new(fixture.Ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(fixture.Draft);
        int originalStrength = snapshot.AbilityScores[Ability.Strength];
        string[] originalSkillProficiencies = snapshot.SkillProficiencies.ToArray();
        string[] originalWeaponIds = snapshot.EquippedWeaponIds.ToArray();
        string[] originalInventoryIds = snapshot.InventoryItems
            .Select(item => item.ItemId)
            .ToArray();
        int originalAttackBonus = Assert.Single(snapshot.WeaponAttacks).AttackBonus;

        fixture.BaseAbilityScores.Clear();
        fixture.BaseAbilityScores[Ability.Strength] = 3;
        fixture.SelectedSkillIds.Clear();
        fixture.SelectedSkillIds.Add("skill.changed");
        fixture.EquippedWeaponIds.Clear();
        fixture.InventoryItems.Clear();

        Assert.Single(fixture.BaseAbilityScores);
        Assert.Equal(3, fixture.BaseAbilityScores[Ability.Strength]);
        Assert.Equal(["skill.changed"], fixture.SelectedSkillIds);
        Assert.Empty(fixture.EquippedWeaponIds);
        Assert.Empty(fixture.InventoryItems);

        Assert.Equal(originalStrength, snapshot.AbilityScores[Ability.Strength]);
        Assert.Equal(originalSkillProficiencies, snapshot.SkillProficiencies);
        Assert.Equal(originalWeaponIds, snapshot.EquippedWeaponIds);
        Assert.Equal(
            originalInventoryIds,
            snapshot.InventoryItems.Select(item => item.ItemId));
        Assert.Equal(originalAttackBonus, Assert.Single(snapshot.WeaponAttacks).AttackBonus);
    }

    private static void AssertProtectedList<T>(IReadOnlyList<T> values)
    {
        Assert.NotEmpty(values);
        Assert.False(values is T[]);
        Assert.False(values is List<T>);

        IList<T> mutableValues = Assert.IsAssignableFrom<IList<T>>(values);
        T firstValue = values[0];

        Assert.Throws<NotSupportedException>(() => mutableValues[0] = firstValue);
        Assert.Equal(firstValue, values[0]);
    }

    private static void AssertProtectedDictionary<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> values,
        TKey existingKey,
        TValue replacementValue)
        where TKey : notnull
    {
        Assert.NotEmpty(values);
        Assert.False(values is Dictionary<TKey, TValue>);

        TValue originalValue = values[existingKey];
        IDictionary<TKey, TValue> mutableValues =
            Assert.IsAssignableFrom<IDictionary<TKey, TValue>>(values);

        Assert.Throws<NotSupportedException>(() =>
            mutableValues[existingKey] = replacementValue);
        Assert.Equal(originalValue, values[existingKey]);
    }

    private static RichCharacterFixture CreateRichCharacterFixture()
    {
        RaceDefinition race = new()
        {
            Id = "race.test",
            Name = "Test Race",
            Size = CharacterSize.Small,
            BaseSpeedFeet = 25,
            AbilityScoreIncreases =
            [
                new AbilityScoreIncrease(Ability.Strength, 1)
            ],
            Languages = ["language.common"],
            Traits = ["trait.brave"],
            Senses =
            [
                new SenseDefinition
                {
                    Type = SenseType.Darkvision,
                    RangeFeet = 60
                }
            ],
            MovementSpeeds =
            [
                new MovementSpeedDefinition
                {
                    Mode = MovementMode.Climb,
                    SpeedFeet = 15
                }
            ],
            DamageResponses =
            [
                new DamageResponseDefinition
                {
                    DamageType = "damage.fire",
                    ResponseType = DamageResponseType.Resistance
                }
            ],
            ConditionImmunities =
            [
                new ConditionImmunityDefinition
                {
                    Condition = ConditionType.Poisoned
                }
            ]
        };

        ClassDefinition characterClass = new()
        {
            Id = "class.test",
            Name = "Test Class",
            HitDie = DieType.D10,
            SavingThrowProficiencies =
            [
                Ability.Strength,
                Ability.Constitution
            ],
            ArmorProficiencies = [RuleIds.ArmorProficiencies.Light],
            WeaponProficiencies = [RuleIds.WeaponProficiencies.Martial],
            ToolProficiencies = ["tool.smith"],
            SkillChoices = ["skill.athletics"],
            NumberOfSkillChoices = 1,
            FeaturesByLevel = new Dictionary<int, IReadOnlyList<string>>
            {
                [1] = ["feature.test"]
            }
        };

        BackgroundDefinition background = new()
        {
            Id = "background.test",
            Name = "Test Background",
            SkillProficiencies = [RuleIds.Skills.Perception],
            ToolProficiencies = ["tool.herbalism"],
            Languages = ["language.dwarvish"],
            FeatureId = "feature.background"
        };

        SkillDefinition athletics = new()
        {
            Id = "skill.athletics",
            Name = "Athletics",
            Ability = Ability.Strength
        };
        SkillDefinition perception = new()
        {
            Id = RuleIds.Skills.Perception,
            Name = "Perception",
            Ability = Ability.Wisdom
        };

        WeaponDefinition weapon = new()
        {
            Id = "weapon.greatsword",
            Name = "Greatsword",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 2,
                Die = DieType.D6
            },
            DamageType = "damage.slashing",
            Properties =
            [
                RuleIds.WeaponProperties.Heavy,
                RuleIds.WeaponProperties.TwoHanded
            ],
            ReachFeet = 5,
            WeightPounds = 6m
        };

        EquipmentItemDefinition equipmentItem = new()
        {
            Id = "item.backpack",
            Name = "Backpack",
            WeightPounds = 5m,
            CostInCopperPieces = 200,
            Tags =
            [
                "equipment_tag.container",
                "equipment_tag.adventuring_gear"
            ]
        };

        RulesetDefinition ruleset = new()
        {
            Id = "ruleset.snapshot_ownership",
            Name = "Snapshot Ownership Ruleset",
            Races = [race],
            Classes = [characterClass],
            Backgrounds = [background],
            Skills = [athletics, perception],
            Weapons = [weapon],
            EquipmentItems = [equipmentItem]
        };

        Dictionary<Ability, int> baseAbilityScores = new()
        {
            [Ability.Strength] = 15,
            [Ability.Dexterity] = 14,
            [Ability.Constitution] = 13,
            [Ability.Intelligence] = 12,
            [Ability.Wisdom] = 10,
            [Ability.Charisma] = 8
        };
        List<string> selectedSkillIds = [athletics.Id];
        List<string> equippedWeaponIds = [weapon.Id];
        List<InventoryItemDraft> inventoryItems =
        [
            new InventoryItemDraft
            {
                ItemId = equipmentItem.Id,
                Quantity = 2
            }
        ];

        CharacterDraft draft = new()
        {
            Name = "Ownership Hero",
            Level = 1,
            RaceId = race.Id,
            ClassId = characterClass.Id,
            BackgroundId = background.Id,
            BaseAbilityScores = baseAbilityScores,
            SelectedSkillIds = selectedSkillIds,
            EquippedWeaponIds = equippedWeaponIds,
            InventoryItems = inventoryItems
        };

        return new RichCharacterFixture(
            ruleset,
            draft,
            baseAbilityScores,
            selectedSkillIds,
            equippedWeaponIds,
            inventoryItems);
    }

    private sealed record RichCharacterFixture(
        RulesetDefinition Ruleset,
        CharacterDraft Draft,
        Dictionary<Ability, int> BaseAbilityScores,
        List<string> SelectedSkillIds,
        List<string> EquippedWeaponIds,
        List<InventoryItemDraft> InventoryItems);
}
