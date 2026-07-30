using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class CampaignPackLoaderTests
{
    private const string FullCampaignPack = """
        {
            "FormatVersion": 1,
            "CampaignId": "campaign.test",
            "DisplayName": "Test Campaign",
            "RulesetId": "ruleset.test",
            "ActivePartySize": 1,
            "Roster": [
                {
                    "PartyMemberId": "party-member.test",
                    "CharacterDefinitionId": "character.test",
                    "DisplayName": "Test Character",
                    "RaceId": "race.human",
                    "ClassId": "class.cleric",
                    "BackgroundId": "background.soldier",
                    "AbilityScores": {
                        "Strength": 11,
                        "Dexterity": 9,
                        "Constitution": 13,
                        "Intelligence": 7,
                        "Wisdom": 15,
                        "Charisma": 12
                    },
                    "SelectedSkillIds": [ "skill.perception", "skill.survival" ],
                    "EquippedWeaponIds": [ "weapon.mace" ],
                    "ZeroHitPointPolicy": "DeathSavingThrows",
                    "MaximumHitPoints": 10,
                    "CurrentHitPoints": 10,
                    "TemporaryHitPoints": 2,
                    "Ammunition": {
                        "WeaponId": "weapon.shortbow",
                        "AmmunitionItemId": "item.arrow",
                        "Quantity": 12
                    },
                    "PreparedSpellIds": [ "spell.sacred-flame", "spell.cure-wounds" ]
                }
            ],
            "ScenarioIds": [ "scenario.test" ]
        }
        """;

    [Fact]
    public void Parse_FullCampaignPack_MapsEveryField()
    {
        CampaignDefinition campaign = CampaignPackLoader.Parse(
            FullCampaignPack);

        Assert.Equal("campaign.test", campaign.CampaignId);
        Assert.Equal("Test Campaign", campaign.DisplayName);
        Assert.Equal("ruleset.test", campaign.RulesetId);
        Assert.Equal(1, campaign.ActivePartySize);
        Assert.Equal(["scenario.test"], campaign.ScenarioIds);

        CampaignCharacterDefinition character = Assert.Single(campaign.Roster);
        Assert.Equal("party-member.test", character.PartyMemberId);
        Assert.Equal("character.test", character.CharacterDefinitionId);
        Assert.Equal("race.human", character.RaceId);
        Assert.Equal("class.cleric", character.ClassId);
        Assert.Equal("background.soldier", character.BackgroundId);

        Assert.Equal(6, character.AbilityScores.Count);
        Assert.Equal(11, character.AbilityScores[Ability.Strength]);
        Assert.Equal(15, character.AbilityScores[Ability.Wisdom]);

        Assert.Equal(
            ["skill.perception", "skill.survival"],
            character.SelectedSkillIds);
        Assert.Equal(["weapon.mace"], character.EquippedWeaponIds);
        Assert.Equal(
            CombatantZeroHitPointPolicy.DeathSavingThrows,
            character.ZeroHitPointPolicy);
        Assert.Equal(10, character.MaximumHitPoints);
        Assert.Equal(10, character.CurrentHitPoints);
        Assert.Equal(2, character.TemporaryHitPoints);

        Assert.NotNull(character.Ammunition);
        Assert.Equal("weapon.shortbow", character.Ammunition.WeaponId);
        Assert.Equal("item.arrow", character.Ammunition.AmmunitionItemId);
        Assert.Equal(12, character.Ammunition.Quantity);

        Assert.Equal(
            ["spell.sacred-flame", "spell.cure-wounds"],
            character.PreparedSpellIds);
    }

    [Fact]
    public void Parse_CharacterWithNoAmmunitionOrSpells_DefaultsCorrectly()
    {
        const string minimalPack = """
            {
                "FormatVersion": 1,
                "CampaignId": "campaign.test",
                "DisplayName": "Test",
                "RulesetId": "ruleset.test",
                "ActivePartySize": 1,
                "Roster": [
                    {
                        "PartyMemberId": "party-member.fighter",
                        "CharacterDefinitionId": "character.fighter",
                        "DisplayName": "Fighter",
                        "RaceId": "race.human",
                        "ClassId": "class.fighter",
                        "BackgroundId": "background.soldier",
                        "AbilityScores": {
                            "Strength": 15,
                            "Dexterity": 11,
                            "Constitution": 13,
                            "Intelligence": 7,
                            "Wisdom": 9,
                            "Charisma": 12
                        },
                        "SelectedSkillIds": [ "skill.athletics" ],
                        "EquippedWeaponIds": [ "weapon.longsword" ],
                        "ZeroHitPointPolicy": "DeathSavingThrows",
                        "MaximumHitPoints": 12,
                        "CurrentHitPoints": 8
                    }
                ],
                "ScenarioIds": []
            }
            """;

        CampaignDefinition campaign = CampaignPackLoader.Parse(minimalPack);
        CampaignCharacterDefinition fighter = Assert.Single(campaign.Roster);

        Assert.Null(fighter.Ammunition);
        Assert.Empty(fighter.PreparedSpellIds);
        Assert.Equal(0, fighter.TemporaryHitPoints);
    }

    [Fact]
    public void Parse_WithUnsupportedFormatVersion_Throws()
    {
        const string futureVersionPack = """
            {
                "FormatVersion": 2,
                "CampaignId": "campaign.test",
                "DisplayName": "Test",
                "RulesetId": "ruleset.test",
                "ActivePartySize": 1,
                "Roster": [],
                "ScenarioIds": []
            }
            """;

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CampaignPackLoader.Parse(futureVersionPack));

        Assert.Contains("format version", exception.Message);
    }

    [Fact]
    public void Load_ReadsFileAndParses()
    {
        string path = Path.GetTempFileName();

        try
        {
            File.WriteAllText(path, FullCampaignPack);

            CampaignDefinition campaign = CampaignPackLoader.Load(path);

            Assert.Equal("campaign.test", campaign.CampaignId);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
