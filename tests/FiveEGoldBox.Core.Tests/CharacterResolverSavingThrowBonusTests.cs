using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class CharacterResolverSavingThrowBonusTests
{
    [Fact]
    public void Resolve_WithClassSavingThrowProficiency_CalculatesProficientSavingThrowBonus()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SavingThrowBonus strengthSave = GetSavingThrowBonus(snapshot, Ability.Strength);

        Assert.Equal(Ability.Strength, strengthSave.Ability);
        Assert.True(strengthSave.IsProficient);
        Assert.Equal(3, strengthSave.AbilityModifier);
        Assert.Equal(2, strengthSave.ProficiencyBonus);
        Assert.Equal(5, strengthSave.TotalBonus);
    }

    [Fact]
    public void Resolve_WithClassSavingThrowProficiency_CalculatesSecondProficientSavingThrowBonus()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SavingThrowBonus constitutionSave = GetSavingThrowBonus(snapshot, Ability.Constitution);

        Assert.Equal(Ability.Constitution, constitutionSave.Ability);
        Assert.True(constitutionSave.IsProficient);
        Assert.Equal(2, constitutionSave.AbilityModifier);
        Assert.Equal(2, constitutionSave.ProficiencyBonus);
        Assert.Equal(4, constitutionSave.TotalBonus);
    }

    [Fact]
    public void Resolve_WithNonProficientSavingThrow_UsesOnlyAbilityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SavingThrowBonus dexteritySave = GetSavingThrowBonus(snapshot, Ability.Dexterity);

        Assert.Equal(Ability.Dexterity, dexteritySave.Ability);
        Assert.False(dexteritySave.IsProficient);
        Assert.Equal(2, dexteritySave.AbilityModifier);
        Assert.Equal(0, dexteritySave.ProficiencyBonus);
        Assert.Equal(2, dexteritySave.TotalBonus);
    }

    [Fact]
    public void Resolve_WithNegativeNonProficientSavingThrow_UsesNegativeAbilityModifier()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SavingThrowBonus charismaSave = GetSavingThrowBonus(snapshot, Ability.Charisma);

        Assert.Equal(Ability.Charisma, charismaSave.Ability);
        Assert.False(charismaSave.IsProficient);
        Assert.Equal(-1, charismaSave.AbilityModifier);
        Assert.Equal(0, charismaSave.ProficiencyBonus);
        Assert.Equal(-1, charismaSave.TotalBonus);
    }

    [Fact]
    public void Resolve_CalculatesOneSavingThrowBonusForEachAbility()
    {
        RulesetDefinition ruleset = CreateTestRuleset();

        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = "race.human",
            ClassId = "class.fighter"
        };

        CharacterResolver resolver = new(ruleset);

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        Assert.Equal(6, snapshot.SavingThrowBonuses.Count);

        foreach (Ability ability in Enum.GetValues<Ability>())
        {
            Assert.Contains(
                snapshot.SavingThrowBonuses,
                savingThrow => savingThrow.Ability == ability);
        }
    }

    [Fact]
    public void Resolve_WithoutSelectedClass_CalculatesNonProficientSavingThrowBonuses()
    {
        CharacterDraft draft = CreateValidDraft() with
        {
            RaceId = null,
            ClassId = null
        };

        CharacterResolver resolver = new();

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        SavingThrowBonus strengthSave = GetSavingThrowBonus(snapshot, Ability.Strength);

        Assert.False(strengthSave.IsProficient);
        Assert.Equal(2, strengthSave.AbilityModifier);
        Assert.Equal(0, strengthSave.ProficiencyBonus);
        Assert.Equal(2, strengthSave.TotalBonus);

        Assert.Equal(6, snapshot.SavingThrowBonuses.Count);
    }

    private static SavingThrowBonus GetSavingThrowBonus(
        CharacterSnapshot snapshot,
        Ability ability)
    {
        return Assert.Single(
            snapshot.SavingThrowBonuses,
            savingThrow => savingThrow.Ability == ability);
    }

    private static CharacterDraft CreateValidDraft()
    {
        return new CharacterDraft
        {
            Name = "Test Fighter",
            Level = 1,
            AbilityScoreGenerationMethod = AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 12,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 8
            }
        };
    }

    private static RulesetDefinition CreateTestRuleset()
    {
        return new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(Ability.Strength, 1),
                        new AbilityScoreIncrease(Ability.Dexterity, 1),
                        new AbilityScoreIncrease(Ability.Constitution, 1),
                        new AbilityScoreIncrease(Ability.Intelligence, 1),
                        new AbilityScoreIncrease(Ability.Wisdom, 1),
                        new AbilityScoreIncrease(Ability.Charisma, 1)
                    ],
                    Languages =
                    [
                        "language.common"
                    ]
                }
            ],
            Classes =
            [
                new ClassDefinition
                {
                    Id = "class.fighter",
                    Name = "Fighter",
                    HitDie = DieType.D10,
                    SavingThrowProficiencies =
                    [
                        Ability.Strength,
                        Ability.Constitution
                    ]
                }
            ]
        };
    }
}
