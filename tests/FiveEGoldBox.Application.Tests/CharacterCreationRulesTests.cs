using FiveEGoldBox.Application.Characters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Persistence;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Exercises CharacterCreationRules against the real, shipped
/// "ruleset.campaign" content (Human/Fighter/Soldier) -- the same content
/// the fixed technical-slice roster resolves against -- rather than a
/// synthetic test ruleset, since the point of this pipeline is that it works
/// against real content, not a fixture built to make it pass.
public sealed class CharacterCreationRulesTests
{
    private const string RulesetId = "ruleset.campaign";

    [Fact]
    public void Validate_LegalDraft_IsValid()
    {
        ValidationResultAssertValid(
            CharacterCreationRules.Validate(
                LegalFighterDraft("Aldric"),
                RulesetId));
    }

    [Fact]
    public void Validate_MissingClass_ReturnsClassRequiredIssue()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            ClassId = null
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.class.required");
    }

    [Fact]
    public void Validate_ManualAbilityGeneration_IsRejected()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Manual
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "character.creation.ability_generation.unsupported");
    }

    [Fact]
    public void Validate_RolledAbilityGeneration_IsRejected()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Rolled
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "character.creation.ability_generation.unsupported");
    }

    [Fact]
    public void Validate_AboveLevelOne_IsRejected()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            Level = 2
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.creation.level.unsupported");
    }

    [Fact]
    public void Validate_BlankRulesetId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CharacterCreationRules.Validate(
                LegalFighterDraft("Aldric"),
                string.Empty));
    }

    [Fact]
    public void Validate_WithSubclassBelongingToItsOwnClass_IsValid()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            ClassId = "class.cleric",
            SelectedSkillIds = ["skill.perception", "skill.survival"],
            SubclassId = "subclass.life-domain"
        };

        ValidationResultAssertValid(
            CharacterCreationRules.Validate(draft, RulesetId));
    }

    [Fact]
    public void Validate_WithSubclassBelongingToADifferentClass_ReturnsSubclassNotFoundIssue()
    {
        // subclass.life-domain belongs to class.cleric, not class.fighter.
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            SubclassId = "subclass.life-domain"
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subclass.not_found");
    }

    [Fact]
    public void CustomBuildParty_WithASubclassChosen_SurfacesItOnTheCharacterSheet()
    {
        List<CharacterCreationEntry> entries = FourLegalEntries();
        entries[0] = entries[0] with
        {
            Draft = entries[0].Draft with
            {
                ClassId = "class.cleric",
                SelectedSkillIds = ["skill.perception", "skill.survival"],
                SubclassId = "subclass.war-domain"
            }
        };

        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            entries,
            RulesetId);

        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            randomSeed: 42,
            party);

        Views.SessionViewModel view = Views.SessionView.Describe(session);

        Views.PartyMemberViewModel resolved = view.Party.Members[0];
        Assert.Equal("War Domain", resolved.SubclassDisplayName);
        Assert.Null(view.Party.Members[1].SubclassDisplayName);
    }

    [Fact]
    public void CreateParty_WithLegalDrafts_ProducesAFullyResolvedParty()
    {
        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            FourLegalEntries(),
            RulesetId);

        Assert.Equal(4, party.Members.Count);

        foreach (PartyMemberState member in party.Members)
        {
            Assert.NotNull(member.CustomBuild);
            Assert.True(member.Health.HitPoints.MaximumHitPoints > 0);
            Assert.Equal(
                member.Health.HitPoints.MaximumHitPoints,
                member.Health.HitPoints.CurrentHitPoints);
            Assert.Equal(0, member.Health.HitPoints.TemporaryHitPoints);
            Assert.Equal(
                CombatantZeroHitPointPolicy.DeathSavingThrows,
                member.ZeroHitPointPolicy);
        }
    }

    [Fact]
    public void CreateParty_WithAnIllegalDraft_Throws()
    {
        List<CharacterCreationEntry> entries = FourLegalEntries();
        entries[2] = entries[2] with
        {
            Draft = entries[2].Draft with { ClassId = "class.does-not-exist" }
        };

        Assert.Throws<InvalidOperationException>(() =>
            CharacterCreationRules.CreateParty(
                "party.custom-test",
                entries,
                RulesetId));
    }

    [Fact]
    public void CreateParty_DuplicatePartyMemberId_Throws()
    {
        List<CharacterCreationEntry> entries = FourLegalEntries();
        entries[1] = entries[1] with
        {
            PartyMemberId = entries[0].PartyMemberId
        };

        Assert.Throws<ArgumentException>(() =>
            CharacterCreationRules.CreateParty(
                "party.custom-test",
                entries,
                RulesetId));
    }

    [Fact]
    public void CreateParty_ThenStartSession_PassesRealCanonicalization()
    {
        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            FourLegalEntries(),
            RulesetId);

        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            randomSeed: 42,
            party);

        Assert.Equal(4, session.Party.Members.Count);
        Assert.Equal(ApplicationMode.Outpost, session.CurrentMode);
    }

    [Fact]
    public void CustomBuildParty_ResolvesARealCharacterSheet()
    {
        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            FourLegalEntries(),
            RulesetId);

        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            randomSeed: 42,
            party);

        // Drives the exact path combat and the character sheet both use to
        // turn a party member back into full stats -- CampaignCharacterDraft
        // Factory.CreateDraft, which for a roster character would look the
        // build up on the campaign; for a custom-build member it has to
        // resolve entirely from PartyMemberState.CustomBuild instead.
        Views.SessionViewModel view = Views.SessionView.Describe(session);

        Assert.Equal(4, view.Party.Members.Count);

        Views.PartyMemberViewModel first = view.Party.Members[0];
        Assert.Equal("Aldric", first.DisplayName);
        Assert.Equal("Human", first.RaceDisplayName);
        Assert.Equal("Fighter", first.ClassDisplayName);
        Assert.Equal(1, first.Level);
        Assert.True(first.ArmorClass > 0);
        Assert.Equal(6, first.AbilityScores.Count);
    }

    [Fact]
    public void CustomBuildParty_SurvivesASaveLoadRoundTrip()
    {
        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            FourLegalEntries(),
            RulesetId);

        ApplicationSessionState session = ScenarioSessionFactory.CreateNew(
            HollowMillScenarioIds.ScenarioId,
            randomSeed: 42,
            party);

        ManualSaveLoadResult result = ManualSaveSerializer.Deserialize(
            ManualSaveSerializer.Serialize(session));

        Assert.True(result.IsSuccess);
        ApplicationSessionState loaded = result.Session!;

        Assert.Equal(4, loaded.Party.Members.Count);

        for (int index = 0; index < session.Party.Members.Count; index++)
        {
            PartyMemberState original = session.Party.Members[index];
            PartyMemberState reloaded = loaded.Party.Members[index];

            Assert.NotNull(reloaded.CustomBuild);
            Assert.Equal(
                original.CustomBuild!.RaceId,
                reloaded.CustomBuild!.RaceId);
            Assert.Equal(
                original.CustomBuild.ClassId,
                reloaded.CustomBuild.ClassId);
            Assert.Equal(
                original.CustomBuild.BackgroundId,
                reloaded.CustomBuild.BackgroundId);
            Assert.Equal(
                original.CustomBuild.AbilityScoreGenerationMethod,
                reloaded.CustomBuild.AbilityScoreGenerationMethod);
            Assert.Equal(
                original.CustomBuild.BaseAbilityScores,
                reloaded.CustomBuild.BaseAbilityScores);
            Assert.Equal(
                original.Health.HitPoints.MaximumHitPoints,
                reloaded.Health.HitPoints.MaximumHitPoints);
        }
    }

    private static void ValidationResultAssertValid(
        Core.Validation.ValidationResult result)
    {
        Assert.True(
            result.IsValid,
            string.Join(
                "; ",
                result.Issues.Select(issue => issue.Message)));
    }

    private static List<CharacterCreationEntry> FourLegalEntries()
    {
        return
        [
            new CharacterCreationEntry
            {
                PartyMemberId = "party-member.custom-test.one",
                Draft = LegalFighterDraft("Aldric")
            },
            new CharacterCreationEntry
            {
                PartyMemberId = "party-member.custom-test.two",
                Draft = LegalFighterDraft("Branwen")
            },
            new CharacterCreationEntry
            {
                PartyMemberId = "party-member.custom-test.three",
                Draft = LegalFighterDraft("Corwin")
            },
            new CharacterCreationEntry
            {
                PartyMemberId = "party-member.custom-test.four",
                Draft = LegalFighterDraft("Delia")
            }
        ];
    }

    /// A legal, level-1 human fighter built against the real shipped
    /// ruleset -- standard array scores, both of the fighter's two class
    /// skill choices (it offers exactly two and requires exactly two), and
    /// its own longsword.
    private static CharacterDraft LegalFighterDraft(string name)
    {
        return new CharacterDraft
        {
            Name = name,
            Level = 1,
            RaceId = "race.human",
            ClassId = "class.fighter",
            BackgroundId = "background.soldier",
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.StandardArray,
            BaseAbilityScores = new Dictionary<Ability, int>
            {
                [Ability.Strength] = 15,
                [Ability.Dexterity] = 14,
                [Ability.Constitution] = 13,
                [Ability.Intelligence] = 8,
                [Ability.Wisdom] = 10,
                [Ability.Charisma] = 12
            },
            SelectedSkillIds =
            [
                "skill.athletics",
                "skill.perception"
            ],
            EquippedWeaponIds = ["weapon.longsword"]
        };
    }
}
