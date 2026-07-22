using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Tests.Builders;

namespace FiveEGoldBox.Core.Tests;

public sealed class RulesetOwnershipTests
{
    [Fact]
    public void Load_AfterTopLevelSourceListsMutate_KeepsCanonicalDefinitionAndIndexStable()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        ValidatedRuleset validated = Load(fixture.Definition);

        Assert.NotSame(fixture.Definition, validated.Definition);
        Assert.Same(validated.Definition, validated.Index.Ruleset);
        Assert.NotSame(fixture.Race, validated.Definition.Races.Single());
        Assert.NotSame(fixture.Class, validated.Definition.Classes.Single());
        Assert.NotSame(fixture.Background, validated.Definition.Backgrounds.Single());
        Assert.NotSame(fixture.Weapon, validated.Definition.Weapons.Single());
        Assert.NotSame(fixture.EquipmentItem, validated.Definition.EquipmentItems.Single());
        Assert.Same(validated.Definition.Races.Single(), validated.Index.RacesById[fixture.Race.Id]);
        Assert.Same(validated.Definition.Classes.Single(), validated.Index.ClassesById[fixture.Class.Id]);
        Assert.Same(
            validated.Definition.Backgrounds.Single(),
            validated.Index.BackgroundsById[fixture.Background.Id]);
        Assert.Same(validated.Definition.Skills.Single(), validated.Index.SkillsById[fixture.Skill.Id]);
        Assert.Same(validated.Definition.Armors.Single(), validated.Index.ArmorsById[fixture.Armor.Id]);
        Assert.Same(validated.Definition.Weapons.Single(), validated.Index.WeaponsById[fixture.Weapon.Id]);
        Assert.Same(
            validated.Definition.EquipmentItems.Single(),
            validated.Index.EquipmentItemsById[fixture.EquipmentItem.Id]);

        fixture.Races.Clear();
        fixture.Classes.Clear();
        fixture.Backgrounds.Clear();
        fixture.Skills.Clear();
        fixture.Armors.Clear();
        fixture.Weapons.Clear();
        fixture.EquipmentItems.Clear();

        Assert.Empty(fixture.Definition.Races);
        Assert.Empty(fixture.Definition.Classes);
        Assert.Empty(fixture.Definition.Backgrounds);
        Assert.Empty(fixture.Definition.Skills);
        Assert.Empty(fixture.Definition.Armors);
        Assert.Empty(fixture.Definition.Weapons);
        Assert.Empty(fixture.Definition.EquipmentItems);

        Assert.Single(validated.Definition.Races);
        Assert.Single(validated.Definition.Classes);
        Assert.Single(validated.Definition.Backgrounds);
        Assert.Single(validated.Definition.Skills);
        Assert.Single(validated.Definition.Armors);
        Assert.Single(validated.Definition.Weapons);
        Assert.Single(validated.Definition.EquipmentItems);
        Assert.Single(validated.Index.RacesById);
        Assert.Single(validated.Index.ClassesById);
        Assert.Single(validated.Index.BackgroundsById);
        Assert.Single(validated.Index.SkillsById);
        Assert.Single(validated.Index.ArmorsById);
        Assert.Single(validated.Index.WeaponsById);
        Assert.Single(validated.Index.EquipmentItemsById);
    }

    [Fact]
    public void Load_AfterRaceAndSubraceSourceCollectionsMutate_KeepsCanonicalContentStable()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        ValidatedRuleset validated = Load(fixture.Definition);

        fixture.RaceAbilityScoreIncreases.Clear();
        fixture.RaceLanguages.Add("language.mutated");
        fixture.RaceTraits.Clear();
        fixture.RaceSubraces.Clear();
        fixture.RaceSenses.Clear();
        fixture.RaceMovementSpeeds.Clear();
        fixture.RaceDamageResponses.Clear();
        fixture.RaceConditionImmunities.Clear();
        fixture.SubraceAbilityScoreIncreases.Clear();
        fixture.SubraceLanguages.Add("language.mutated");
        fixture.SubraceTraits.Clear();
        fixture.SubraceSenses.Clear();
        fixture.SubraceMovementSpeeds.Clear();
        fixture.SubraceDamageResponses.Clear();
        fixture.SubraceConditionImmunities.Clear();

        Assert.Empty(fixture.RaceAbilityScoreIncreases);
        Assert.Contains("language.mutated", fixture.RaceLanguages);
        Assert.Empty(fixture.RaceTraits);
        Assert.Empty(fixture.RaceSubraces);
        Assert.Empty(fixture.RaceSenses);
        Assert.Empty(fixture.RaceMovementSpeeds);
        Assert.Empty(fixture.RaceDamageResponses);
        Assert.Empty(fixture.RaceConditionImmunities);
        Assert.Empty(fixture.SubraceAbilityScoreIncreases);
        Assert.Contains("language.mutated", fixture.SubraceLanguages);
        Assert.Empty(fixture.SubraceTraits);
        Assert.Empty(fixture.SubraceSenses);
        Assert.Empty(fixture.SubraceMovementSpeeds);
        Assert.Empty(fixture.SubraceDamageResponses);
        Assert.Empty(fixture.SubraceConditionImmunities);

        RaceDefinition race = Assert.Single(validated.Definition.Races);
        SubraceDefinition subrace = Assert.Single(race.Subraces);

        Assert.Equal(new AbilityScoreIncrease(Ability.Strength, 1), Assert.Single(race.AbilityScoreIncreases));
        Assert.Equal(["language.common"], race.Languages);
        Assert.Equal(["trait.test"], race.Traits);
        Assert.Equal(SenseType.Darkvision, Assert.Single(race.Senses).Type);
        Assert.Equal(MovementMode.Walk, Assert.Single(race.MovementSpeeds).Mode);
        Assert.Equal(DamageResponseType.Resistance, Assert.Single(race.DamageResponses).ResponseType);
        Assert.Equal(ConditionType.Poisoned, Assert.Single(race.ConditionImmunities).Condition);
        Assert.Equal(
            new AbilityScoreIncrease(Ability.Dexterity, 1),
            Assert.Single(subrace.AbilityScoreIncreases));
        Assert.Equal(["language.subrace"], subrace.Languages);
        Assert.Equal(["trait.subrace"], subrace.Traits);
        Assert.Equal(SenseType.Blindsight, Assert.Single(subrace.Senses).Type);
        Assert.Equal(MovementMode.Climb, Assert.Single(subrace.MovementSpeeds).Mode);
        Assert.Equal(DamageResponseType.Immunity, Assert.Single(subrace.DamageResponses).ResponseType);
        Assert.Equal(ConditionType.Frightened, Assert.Single(subrace.ConditionImmunities).Condition);
    }

    [Fact]
    public void Load_AfterClassSourceCollectionsMutate_KeepsOuterAndInnerFeaturesStable()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        ValidatedRuleset validated = Load(fixture.Definition);

        fixture.SavingThrowProficiencies.Clear();
        fixture.ArmorProficiencies.Clear();
        fixture.WeaponProficiencies.Clear();
        fixture.ToolProficiencies.Clear();
        fixture.SkillChoices.Clear();
        fixture.LevelOneFeatures.Add("feature.mutated");
        fixture.FeaturesByLevel.Remove(1);
        fixture.FeaturesByLevel[2] = new List<string> { "feature.level-two" };

        Assert.Empty(fixture.SavingThrowProficiencies);
        Assert.Empty(fixture.ArmorProficiencies);
        Assert.Empty(fixture.WeaponProficiencies);
        Assert.Empty(fixture.ToolProficiencies);
        Assert.Empty(fixture.SkillChoices);
        Assert.Contains("feature.mutated", fixture.LevelOneFeatures);
        Assert.False(fixture.FeaturesByLevel.ContainsKey(1));

        ClassDefinition characterClass = Assert.Single(validated.Definition.Classes);

        Assert.Equal([Ability.Strength], characterClass.SavingThrowProficiencies);
        Assert.Equal([RuleIds.ArmorProficiencies.Light], characterClass.ArmorProficiencies);
        Assert.Equal([RuleIds.WeaponProficiencies.Martial], characterClass.WeaponProficiencies);
        Assert.Equal(["tool.test"], characterClass.ToolProficiencies);
        Assert.Equal([fixture.Skill.Id], characterClass.SkillChoices);
        Assert.Single(characterClass.FeaturesByLevel);
        Assert.Equal(["feature.level-one"], characterClass.FeaturesByLevel[1]);

        IDictionary<int, IReadOnlyList<string>> features =
            Assert.IsAssignableFrom<IDictionary<int, IReadOnlyList<string>>>(
                characterClass.FeaturesByLevel);
        Assert.Throws<NotSupportedException>(() => features.Add(3, ["feature.external"]));

        IList<string> levelOneFeatures =
            Assert.IsAssignableFrom<IList<string>>(characterClass.FeaturesByLevel[1]);
        Assert.Throws<NotSupportedException>(() => levelOneFeatures.Add("feature.external"));
    }

    [Fact]
    public void Load_AfterBackgroundWeaponAndEquipmentCollectionsMutate_KeepsCanonicalContentStable()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        ValidatedRuleset validated = Load(fixture.Definition);

        fixture.BackgroundSkillProficiencies.Clear();
        fixture.BackgroundToolProficiencies.Clear();
        fixture.BackgroundLanguages.Clear();
        fixture.WeaponProperties.Clear();
        fixture.EquipmentTags.Clear();

        Assert.Empty(fixture.BackgroundSkillProficiencies);
        Assert.Empty(fixture.BackgroundToolProficiencies);
        Assert.Empty(fixture.BackgroundLanguages);
        Assert.Empty(fixture.WeaponProperties);
        Assert.Empty(fixture.EquipmentTags);

        BackgroundDefinition background = Assert.Single(validated.Definition.Backgrounds);
        WeaponDefinition weapon = Assert.Single(validated.Definition.Weapons);
        EquipmentItemDefinition equipmentItem = Assert.Single(validated.Definition.EquipmentItems);

        Assert.Equal([fixture.Skill.Id], background.SkillProficiencies);
        Assert.Equal(["tool.test"], background.ToolProficiencies);
        Assert.Equal(["language.background"], background.Languages);
        Assert.Equal([RuleIds.WeaponProperties.Finesse], weapon.Properties);
        Assert.Equal(["tag.test"], equipmentItem.Tags);
    }

    [Fact]
    public void Load_IndexDictionariesCannotBeMutatedThroughConcreteOrMutableDictionaryCasts()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        ValidatedRuleset validated = Load(fixture.Definition);

        AssertIndexProtected(validated.Index.RacesById, fixture.Race.Id, fixture.Race);
        AssertIndexProtected(validated.Index.ClassesById, fixture.Class.Id, fixture.Class);
        AssertIndexProtected(
            validated.Index.BackgroundsById,
            fixture.Background.Id,
            fixture.Background);
        AssertIndexProtected(validated.Index.SkillsById, fixture.Skill.Id, fixture.Skill);
        AssertIndexProtected(validated.Index.ArmorsById, fixture.Armor.Id, fixture.Armor);
        AssertIndexProtected(validated.Index.WeaponsById, fixture.Weapon.Id, fixture.Weapon);
        AssertIndexProtected(
            validated.Index.EquipmentItemsById,
            fixture.EquipmentItem.Id,
            fixture.EquipmentItem);

        Assert.Same(validated.Definition.Races.Single(), validated.Index.RacesById[fixture.Race.Id]);
    }

    [Fact]
    public void RulesetIndex_AfterSourceMutation_KeepsOneProtectedCanonicalGraph()
    {
        MutableRulesetFixture fixture = CreateMutableFixture();
        RulesetIndex index = new(fixture.Definition);

        fixture.Races.Clear();
        fixture.RaceLanguages.Add("language.mutated");
        fixture.WeaponProperties.Clear();

        Assert.NotSame(fixture.Definition, index.Ruleset);
        RaceDefinition race = Assert.Single(index.Ruleset.Races);
        WeaponDefinition weapon = Assert.Single(index.Ruleset.Weapons);
        Assert.Equal(["language.common"], race.Languages);
        Assert.Equal([RuleIds.WeaponProperties.Finesse], weapon.Properties);
        Assert.Same(race, index.RacesById[fixture.Race.Id]);
        Assert.Same(weapon, index.WeaponsById[fixture.Weapon.Id]);
    }

    [Fact]
    public void CharacterResolver_WithValidatedRuleset_RemainsStableAfterSourceMutation()
    {
        MutableResolverFixture fixture = CreateMutableResolverFixture();
        ValidatedRuleset validated = Load(fixture.Definition);
        CharacterResolver resolver = new(validated);

        MutateResolverSources(fixture);

        CharacterSnapshot snapshot = resolver.Resolve(CreateResolverDraft());

        Assert.Equal(16, snapshot.AbilityScores[Ability.Strength]);
        Assert.Contains("skill.athletics", snapshot.SkillProficiencies);
        Assert.Contains("skill.perception", snapshot.SkillProficiencies);
    }

    [Fact]
    public void CharacterResolver_WithDirectRuleset_RemainsStableAfterSourceMutation()
    {
        MutableResolverFixture fixture = CreateMutableResolverFixture();
        CharacterResolver resolver = new(fixture.Definition);

        MutateResolverSources(fixture);

        CharacterSnapshot snapshot = resolver.Resolve(CreateResolverDraft());

        Assert.Equal(16, snapshot.AbilityScores[Ability.Strength]);
        Assert.Contains("skill.athletics", snapshot.SkillProficiencies);
        Assert.Contains("skill.perception", snapshot.SkillProficiencies);
    }

    private static void AssertIndexProtected<TDefinition>(
        IReadOnlyDictionary<string, TDefinition> index,
        string existingId,
        TDefinition replacement)
    {
        Assert.Null(index as Dictionary<string, TDefinition>);

        IDictionary<string, TDefinition> mutable =
            Assert.IsAssignableFrom<IDictionary<string, TDefinition>>(index);

        Assert.Throws<NotSupportedException>(() => mutable.Remove(existingId));
        Assert.Throws<NotSupportedException>(() => mutable["definition.external"] = replacement);
        Assert.True(index.ContainsKey(existingId));
        Assert.False(index.ContainsKey("definition.external"));
    }

    private static ValidatedRuleset Load(RulesetDefinition definition)
    {
        RulesetLoadResult result = ValidatedRuleset.Load(definition);

        Assert.True(result.IsValid);
        return Assert.IsType<ValidatedRuleset>(result.Ruleset);
    }

    private static CharacterDraft CreateResolverDraft()
    {
        return new TestCharacterDraftBuilder()
            .WithBackgroundId(null)
            .WithSelectedSkillIds(
            [
                "skill.athletics",
                "skill.perception"
            ])
            .Build();
    }

    private static MutableResolverFixture CreateMutableResolverFixture()
    {
        List<AbilityScoreIncrease> abilityScoreIncreases =
        [
            new AbilityScoreIncrease(Ability.Strength, 1),
            new AbilityScoreIncrease(Ability.Dexterity, 1),
            new AbilityScoreIncrease(Ability.Constitution, 1),
            new AbilityScoreIncrease(Ability.Intelligence, 1),
            new AbilityScoreIncrease(Ability.Wisdom, 1),
            new AbilityScoreIncrease(Ability.Charisma, 1)
        ];

        RaceDefinition race = TestRulesetBuilder.HumanRace() with
        {
            AbilityScoreIncreases = abilityScoreIncreases
        };

        List<string> skillChoices = TestRulesetBuilder.FighterClass().SkillChoices.ToList();
        ClassDefinition characterClass = TestRulesetBuilder.FighterClass() with
        {
            SkillChoices = skillChoices
        };

        List<RaceDefinition> races = [race];
        List<ClassDefinition> classes = [characterClass];

        RulesetDefinition definition = new TestRulesetBuilder()
            .WithRaces(races)
            .WithClasses(classes)
            .Build();

        return new MutableResolverFixture(
            definition,
            races,
            classes,
            abilityScoreIncreases,
            skillChoices);
    }

    private static void MutateResolverSources(MutableResolverFixture fixture)
    {
        fixture.Races.Clear();
        fixture.Classes.Clear();
        fixture.AbilityScoreIncreases.Clear();
        fixture.SkillChoices.Clear();

        Assert.Empty(fixture.Definition.Races);
        Assert.Empty(fixture.Definition.Classes);
        Assert.Empty(fixture.AbilityScoreIncreases);
        Assert.Empty(fixture.SkillChoices);
    }

    private static MutableRulesetFixture CreateMutableFixture()
    {
        List<AbilityScoreIncrease> raceAbilityScoreIncreases =
        [
            new AbilityScoreIncrease(Ability.Strength, 1)
        ];
        List<string> raceLanguages = ["language.common"];
        List<string> raceTraits = ["trait.test"];
        List<SenseDefinition> raceSenses =
        [
            new SenseDefinition
            {
                Type = SenseType.Darkvision,
                RangeFeet = 60
            }
        ];
        List<MovementSpeedDefinition> raceMovementSpeeds =
        [
            new MovementSpeedDefinition
            {
                Mode = MovementMode.Walk,
                SpeedFeet = 30
            }
        ];
        List<DamageResponseDefinition> raceDamageResponses =
        [
            new DamageResponseDefinition
            {
                DamageType = "damage.fire",
                ResponseType = DamageResponseType.Resistance
            }
        ];
        List<ConditionImmunityDefinition> raceConditionImmunities =
        [
            new ConditionImmunityDefinition
            {
                Condition = ConditionType.Poisoned
            }
        ];

        List<AbilityScoreIncrease> subraceAbilityScoreIncreases =
        [
            new AbilityScoreIncrease(Ability.Dexterity, 1)
        ];
        List<string> subraceLanguages = ["language.subrace"];
        List<string> subraceTraits = ["trait.subrace"];
        List<SenseDefinition> subraceSenses =
        [
            new SenseDefinition
            {
                Type = SenseType.Blindsight,
                RangeFeet = 10
            }
        ];
        List<MovementSpeedDefinition> subraceMovementSpeeds =
        [
            new MovementSpeedDefinition
            {
                Mode = MovementMode.Climb,
                SpeedFeet = 20
            }
        ];
        List<DamageResponseDefinition> subraceDamageResponses =
        [
            new DamageResponseDefinition
            {
                DamageType = "damage.cold",
                ResponseType = DamageResponseType.Immunity
            }
        ];
        List<ConditionImmunityDefinition> subraceConditionImmunities =
        [
            new ConditionImmunityDefinition
            {
                Condition = ConditionType.Frightened
            }
        ];

        SubraceDefinition subrace = new()
        {
            Id = "subrace.test",
            Name = "Test Subrace",
            AbilityScoreIncreases = subraceAbilityScoreIncreases,
            Languages = subraceLanguages,
            Traits = subraceTraits,
            Senses = subraceSenses,
            MovementSpeeds = subraceMovementSpeeds,
            DamageResponses = subraceDamageResponses,
            ConditionImmunities = subraceConditionImmunities
        };
        List<SubraceDefinition> raceSubraces = [subrace];

        RaceDefinition race = new()
        {
            Id = "race.test",
            Name = "Test Race",
            BaseSpeedFeet = 30,
            AbilityScoreIncreases = raceAbilityScoreIncreases,
            Languages = raceLanguages,
            Traits = raceTraits,
            Subraces = raceSubraces,
            Senses = raceSenses,
            MovementSpeeds = raceMovementSpeeds,
            DamageResponses = raceDamageResponses,
            ConditionImmunities = raceConditionImmunities
        };

        SkillDefinition skill = new()
        {
            Id = "skill.athletics",
            Name = "Athletics",
            Ability = Ability.Strength
        };

        List<Ability> savingThrowProficiencies = [Ability.Strength];
        List<string> armorProficiencies = [RuleIds.ArmorProficiencies.Light];
        List<string> weaponProficiencies = [RuleIds.WeaponProficiencies.Martial];
        List<string> toolProficiencies = ["tool.test"];
        List<string> skillChoices = [skill.Id];
        List<string> levelOneFeatures = ["feature.level-one"];
        Dictionary<int, IReadOnlyList<string>> featuresByLevel = new()
        {
            [1] = levelOneFeatures
        };

        ClassDefinition characterClass = new()
        {
            Id = "class.test",
            Name = "Test Class",
            HitDie = DieType.D10,
            SavingThrowProficiencies = savingThrowProficiencies,
            ArmorProficiencies = armorProficiencies,
            WeaponProficiencies = weaponProficiencies,
            ToolProficiencies = toolProficiencies,
            SkillChoices = skillChoices,
            NumberOfSkillChoices = 1,
            FeaturesByLevel = featuresByLevel
        };

        List<string> backgroundSkillProficiencies = [skill.Id];
        List<string> backgroundToolProficiencies = ["tool.test"];
        List<string> backgroundLanguages = ["language.background"];
        BackgroundDefinition background = new()
        {
            Id = "background.test",
            Name = "Test Background",
            SkillProficiencies = backgroundSkillProficiencies,
            ToolProficiencies = backgroundToolProficiencies,
            Languages = backgroundLanguages,
            FeatureId = "feature.background"
        };

        ArmorDefinition armor = new()
        {
            Id = "armor.test",
            Name = "Test Armor",
            Category = ArmorCategory.Light,
            BaseArmorClass = 11,
            AddsDexterityModifier = true
        };

        List<string> weaponProperties = [RuleIds.WeaponProperties.Finesse];
        WeaponDefinition weapon = new()
        {
            Id = "weapon.test",
            Name = "Test Weapon",
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = DieType.D6
            },
            DamageType = "damage.slashing",
            Properties = weaponProperties,
            ReachFeet = 5
        };

        List<string> equipmentTags = ["tag.test"];
        EquipmentItemDefinition equipmentItem = new()
        {
            Id = "item.test",
            Name = "Test Item",
            Tags = equipmentTags
        };

        List<RaceDefinition> races = [race];
        List<ClassDefinition> classes = [characterClass];
        List<BackgroundDefinition> backgrounds = [background];
        List<SkillDefinition> skills = [skill];
        List<ArmorDefinition> armors = [armor];
        List<WeaponDefinition> weapons = [weapon];
        List<EquipmentItemDefinition> equipmentItems = [equipmentItem];

        RulesetDefinition definition = new()
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races = races,
            Classes = classes,
            Backgrounds = backgrounds,
            Skills = skills,
            Armors = armors,
            Weapons = weapons,
            EquipmentItems = equipmentItems
        };

        return new MutableRulesetFixture(
            definition,
            race,
            subrace,
            characterClass,
            background,
            skill,
            armor,
            weapon,
            equipmentItem,
            races,
            classes,
            backgrounds,
            skills,
            armors,
            weapons,
            equipmentItems,
            raceAbilityScoreIncreases,
            raceLanguages,
            raceTraits,
            raceSubraces,
            raceSenses,
            raceMovementSpeeds,
            raceDamageResponses,
            raceConditionImmunities,
            subraceAbilityScoreIncreases,
            subraceLanguages,
            subraceTraits,
            subraceSenses,
            subraceMovementSpeeds,
            subraceDamageResponses,
            subraceConditionImmunities,
            savingThrowProficiencies,
            armorProficiencies,
            weaponProficiencies,
            toolProficiencies,
            skillChoices,
            featuresByLevel,
            levelOneFeatures,
            backgroundSkillProficiencies,
            backgroundToolProficiencies,
            backgroundLanguages,
            weaponProperties,
            equipmentTags);
    }

    private sealed record MutableResolverFixture(
        RulesetDefinition Definition,
        List<RaceDefinition> Races,
        List<ClassDefinition> Classes,
        List<AbilityScoreIncrease> AbilityScoreIncreases,
        List<string> SkillChoices);

    private sealed record MutableRulesetFixture(
        RulesetDefinition Definition,
        RaceDefinition Race,
        SubraceDefinition Subrace,
        ClassDefinition Class,
        BackgroundDefinition Background,
        SkillDefinition Skill,
        ArmorDefinition Armor,
        WeaponDefinition Weapon,
        EquipmentItemDefinition EquipmentItem,
        List<RaceDefinition> Races,
        List<ClassDefinition> Classes,
        List<BackgroundDefinition> Backgrounds,
        List<SkillDefinition> Skills,
        List<ArmorDefinition> Armors,
        List<WeaponDefinition> Weapons,
        List<EquipmentItemDefinition> EquipmentItems,
        List<AbilityScoreIncrease> RaceAbilityScoreIncreases,
        List<string> RaceLanguages,
        List<string> RaceTraits,
        List<SubraceDefinition> RaceSubraces,
        List<SenseDefinition> RaceSenses,
        List<MovementSpeedDefinition> RaceMovementSpeeds,
        List<DamageResponseDefinition> RaceDamageResponses,
        List<ConditionImmunityDefinition> RaceConditionImmunities,
        List<AbilityScoreIncrease> SubraceAbilityScoreIncreases,
        List<string> SubraceLanguages,
        List<string> SubraceTraits,
        List<SenseDefinition> SubraceSenses,
        List<MovementSpeedDefinition> SubraceMovementSpeeds,
        List<DamageResponseDefinition> SubraceDamageResponses,
        List<ConditionImmunityDefinition> SubraceConditionImmunities,
        List<Ability> SavingThrowProficiencies,
        List<string> ArmorProficiencies,
        List<string> WeaponProficiencies,
        List<string> ToolProficiencies,
        List<string> SkillChoices,
        Dictionary<int, IReadOnlyList<string>> FeaturesByLevel,
        List<string> LevelOneFeatures,
        List<string> BackgroundSkillProficiencies,
        List<string> BackgroundToolProficiencies,
        List<string> BackgroundLanguages,
        List<string> WeaponProperties,
        List<string> EquipmentTags);
}
