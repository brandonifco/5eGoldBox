using FiveEGoldBox.Application.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// Guards the content behind character creation: every choice a player is
/// offered has to arrive with prose explaining it, and every class has to say
/// which abilities it runs on.
///
/// These exist because the failure mode is silent. Description and
/// PrimaryAbilities are optional on the Core definitions -- deliberately, so
/// content predating them still loads -- and they cross into the runtime
/// through the hand-mirrored Content/V1 DTO layer, where an unmapped property
/// is dropped rather than rejected. Without these tests, authoring a new race
/// and forgetting its description, or adding a DTO field and forgetting to map
/// it, both produce a creation screen that quietly shows a bare name again.
public sealed class CampaignCharacterChoiceContentTests
{
    private const string RulesetId = "ruleset.campaign";

    private static CharacterCreationOptions Options =>
        CharacterCreationRules.DescribeOptions(RulesetId);

    [Fact]
    public void EveryRaceAndSubrace_HasADescription()
    {
        foreach (RaceDefinition race in Options.Races)
        {
            AssertDescribed(race.Description, $"Race '{race.Id}'");

            foreach (SubraceDefinition subrace in race.Subraces)
            {
                AssertDescribed(
                    subrace.Description,
                    $"Subrace '{subrace.Id}'");
            }
        }
    }

    [Fact]
    public void EveryClassAndSubclass_HasADescription()
    {
        foreach (ClassDefinition characterClass in Options.Classes)
        {
            AssertDescribed(
                characterClass.Description,
                $"Class '{characterClass.Id}'");

            foreach (SubclassDefinition subclass in characterClass.Subclasses)
            {
                AssertDescribed(
                    subclass.Description,
                    $"Subclass '{subclass.Id}'");
            }
        }
    }

    [Fact]
    public void EveryBackground_HasADescription()
    {
        foreach (BackgroundDefinition background in Options.Backgrounds)
        {
            AssertDescribed(
                background.Description,
                $"Background '{background.Id}'");
        }
    }

    [Fact]
    public void EverySkill_HasADescription()
    {
        foreach (SkillDefinition skill in Options.Skills)
        {
            AssertDescribed(skill.Description, $"Skill '{skill.Id}'");
        }
    }

    /// The input a creation screen's recommendations are derived from. A class
    /// with none would silently recommend nothing rather than fail.
    [Fact]
    public void EveryClass_DeclaresItsPrimaryAbilities()
    {
        foreach (ClassDefinition characterClass in Options.Classes)
        {
            Assert.True(
                characterClass.PrimaryAbilities.Count > 0,
                $"Class '{characterClass.Id}' declares no primary abilities.");

            Assert.Equal(
                characterClass.PrimaryAbilities.Distinct().Count(),
                characterClass.PrimaryAbilities.Count);

            foreach (Ability ability in characterClass.PrimaryAbilities)
            {
                Assert.True(
                    Enum.IsDefined(ability),
                    $"Class '{characterClass.Id}' names an undefined ability.");
            }
        }
    }

    /// A caster's spellcasting ability is the one it most runs on, so the two
    /// facts must not contradict each other -- a Wizard recommending Wisdom
    /// while casting on Intelligence would actively mislead a new player.
    [Fact]
    public void ACastersPrimaryAbility_IsItsSpellcastingAbility()
    {
        foreach (ClassDefinition characterClass in Options.Classes)
        {
            if (characterClass.SpellcastingAbility is not Ability casting)
            {
                continue;
            }

            Assert.Equal(casting, characterClass.PrimaryAbilities[0]);
        }
    }

    private static void AssertDescribed(string? description, string subject)
    {
        Assert.False(
            string.IsNullOrWhiteSpace(description),
            $"{subject} has no description, so a creation screen can only "
                + "show its bare name.");
    }
}
