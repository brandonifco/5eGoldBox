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
            SelectedSkillIds = ["skill.insight", "skill.religion"],
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
                SelectedSkillIds = ["skill.insight", "skill.religion"],
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

    /// A race that declares subraces must have one chosen -- the rule existed
    /// long before any shipped race had a subrace to choose, so until the
    /// dwarf/elf/halfling content landed nothing exercised it against real
    /// content.
    [Fact]
    public void Validate_RaceWithSubraces_RequiresOneToBeChosen()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            RaceId = "race.dwarf"
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.subrace.required");
    }

    /// Race and subrace ability-score increases stack, and a race's own
    /// BaseSpeedFeet is no longer uniformly 30 -- both only became observable
    /// once a second race existed.
    [Fact]
    public void CustomBuildParty_MountainDwarf_StacksRaceAndSubraceIncreases()
    {
        List<CharacterCreationEntry> entries = FourLegalEntries();
        entries[0] = entries[0] with
        {
            Draft = entries[0].Draft with
            {
                RaceId = "race.dwarf",
                SubraceId = "subrace.mountain-dwarf"
            }
        };

        PartyState party = CharacterCreationRules.CreateParty(
            "party.custom-test",
            entries,
            RulesetId);

        Views.SessionViewModel view = Views.SessionView.Describe(
            ScenarioSessionFactory.CreateNew(
                HollowMillScenarioIds.ScenarioId,
                randomSeed: 42,
                party));

        Views.PartyMemberViewModel resolved = view.Party.Members[0];

        Assert.Equal("Dwarf", resolved.RaceDisplayName);
        Assert.Equal(25, resolved.SpeedFeet);

        // Base 15 Strength + 2 from Mountain Dwarf, base 13 Constitution
        // + 2 from Dwarf itself.
        Assert.Equal(17, ScoreOf(resolved, Ability.Strength));
        Assert.Equal(15, ScoreOf(resolved, Ability.Constitution));

        // The human fighter beside them keeps the human's own speed and its
        // +1-to-everything increase (base 15 Strength + 1), rather than
        // picking up anything the dwarf brought.
        Assert.Equal(30, view.Party.Members[1].SpeedFeet);
        Assert.Equal(
            16,
            ScoreOf(view.Party.Members[1], Ability.Strength));
    }

    /// Class skill lists are wider than the number a class may choose, so a
    /// selection off its own list is now a real, reachable rejection rather
    /// than an unreachable rule -- every class previously offered exactly as
    /// many skills as it required.
    [Fact]
    public void Validate_SkillOutsideTheClassList_IsRejected()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            ClassId = "class.wizard",

            // Both are real skills, and neither is on the wizard's list.
            SelectedSkillIds = ["skill.athletics", "skill.stealth"]
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.skills.not_available");
    }

    /// A background contributes skill proficiencies on top of the class's own
    /// chosen ones. Backgrounds carried no mechanical payload at all until the
    /// acolyte/criminal/sage content landed beside the soldier.
    [Fact]
    public void Resolve_BackgroundSkillProficiencies_AddToTheClassChoices()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            ClassId = "class.wizard",
            BackgroundId = "background.sage",
            SelectedSkillIds = ["skill.insight", "skill.investigation"]
        };

        CharacterResolver resolver = new(
            Scenarios.RulesetRegistry.Resolve(RulesetId));

        CharacterSnapshot snapshot = resolver.Resolve(draft);

        IReadOnlyList<string> proficient = snapshot.SkillBonuses
            .Where(skill => skill.IsProficient)
            .Select(skill => skill.SkillId)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "skill.arcana",
                "skill.history",
                "skill.insight",
                "skill.investigation"
            ],
            proficient);
    }

    /// The read half of creation. Resolving a ruleset is internal to
    /// Application, so without this a client could validate a draft it had no
    /// way to build the choices for.
    [Fact]
    public void DescribeOptions_ReturnsTheRealShippedContent()
    {
        CharacterCreationOptions options =
            CharacterCreationRules.DescribeOptions(RulesetId);

        Assert.Contains(options.Races, race => race.Id == "race.dwarf");
        Assert.Contains(options.Classes, cls => cls.Id == "class.wizard");
        Assert.Contains(
            options.Backgrounds,
            background => background.Id == "background.sage");
        Assert.Contains(options.Skills, skill => skill.Id == "skill.arcana");
        Assert.Contains(options.Spells, spell => spell.Id == "spell.bless");

        // A client renders choices in the order content declares them, so the
        // ruleset's own authored order is preserved rather than sorted.
        Assert.Equal("race.human", options.Races[0].Id);

        // Enough shape to drive a real wizard: a race's subraces and a class's
        // own skill list both arrive with it.
        Assert.Equal(
            2,
            options.Races
                .Single(race => race.Id == "race.dwarf")
                .Subraces.Count);

        Assert.Equal(
            8,
            options.Classes
                .Single(cls => cls.Id == "class.fighter")
                .SkillChoices.Count);
    }

    [Fact]
    public void DescribeOptions_WithNoRulesetId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CharacterCreationRules.DescribeOptions(string.Empty));
    }

    [Fact]
    public void Validate_PreparedSpellOnANonCaster_IsRejected()
    {
        CharacterDraft draft = LegalFighterDraft("Aldric") with
        {
            PreparedSpellIds = ["spell.bless"]
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.spells.not_a_caster");
    }

    [Fact]
    public void Validate_UnknownPreparedSpell_IsRejected()
    {
        CharacterDraft draft = LegalClericDraft("Aldric") with
        {
            PreparedSpellIds = ["spell.does-not-exist"]
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.spells.not_found");
    }

    [Fact]
    public void Validate_DuplicatePreparedSpells_AreRejected()
    {
        CharacterDraft draft = LegalClericDraft("Aldric") with
        {
            PreparedSpellIds = ["spell.bless", "spell.bless"]
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.spells.duplicate");
    }

    [Fact]
    public void Validate_RealPreparedSpellsOnACaster_AreAccepted()
    {
        CharacterDraft draft = LegalClericDraft("Aldric") with
        {
            PreparedSpellIds = ["spell.bless", "spell.cure-wounds"]
        };

        ValidationResultAssertValid(
            CharacterCreationRules.Validate(draft, RulesetId));
    }

    /// Fire Bolt is a real, existing spell -- just not one class.cleric's
    /// own list names. A Cleric preparing it should be rejected the same
    /// deliberate way an unknown spell ID is, not silently accepted just
    /// because the spell itself is real.
    [Fact]
    public void Validate_PreparedSpellNotOnTheClassList_IsRejected()
    {
        CharacterDraft draft = LegalClericDraft("Aldric") with
        {
            PreparedSpellIds = ["spell.fire-bolt"]
        };

        Core.Validation.ValidationResult result =
            CharacterCreationRules.Validate(draft, RulesetId);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "character.spells.not_on_class_list");
    }

    private static CharacterDraft LegalClericDraft(string name)
    {
        return LegalFighterDraft(name) with
        {
            ClassId = "class.cleric",
            SelectedSkillIds = ["skill.insight", "skill.religion"]
        };
    }

    private static int ScoreOf(
        Views.PartyMemberViewModel member,
        Ability ability)
    {
        return member.AbilityScores
            .Single(score => score.AbilityName == ability.ToString())
            .Score;
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
    /// ruleset -- standard array scores, two of the eight skills the fighter
    /// offers (it requires exactly two), and its own longsword.
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
