using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// Sneak Attack as authored content, and what it does once a character has it.
///
/// This is the step that answers whether the roll-contribution seam is
/// secretly spell-shaped: nothing here is a spell, nothing installs an effect,
/// and the extra damage arrives from what the character *is*.
public sealed class CampaignFeatureContentTests
{
    [Fact]
    public void TheCampaignRulesetValidatesWithNoIssuesAtAll()
    {
        ValidationResult validation = RulesetValidator.Validate(
            RulesetRegistry
                .Resolve(RulesetRegistry.CampaignRulesetId)
                .Definition);

        Assert.Empty(validation.Issues);
    }

    [Fact]
    public void SneakAttack_IsExtraDamageRatherThanAnythingSpellLike()
    {
        RollContributionDefinition contribution = Assert.Single(
            Feature(CampaignRulesetIds.SneakAttackFeatureId)
                .Contributions);

        Assert.Equal(
            RollContributionTarget.DamageRoll,
            contribution.Target);
        Assert.Equal(1, contribution.Dice!.Count);
        Assert.Equal(DieType.D6, contribution.Dice.Die);
        Assert.Equal(0, contribution.FlatBonus);
    }

    /// Both halves of 5e's trigger, and both required — a rogue needs an
    /// opening *and* a weapon it can be sneaky with.
    [Fact]
    public void SneakAttack_AsksForAnOpeningAndTheRightWeapon()
    {
        Assert.Equal(
            [
                RollContributionCondition.AdvantageOrAdjacentEnemy,
                RollContributionCondition.FinesseOrRangedWeapon
            ],
            Assert.Single(
                Feature(CampaignRulesetIds.SneakAttackFeatureId)
                    .Contributions)
                .Conditions);
    }

    [Fact]
    public void TheRogueClassNamesSneakAttackAtLevelOne()
    {
        ClassDefinition rogue = Assert.Single(
            RulesetRegistry
                .Resolve(RulesetRegistry.CampaignRulesetId)
                .Definition
                .Classes,
            characterClass => characterClass.Id
                == CampaignRulesetIds.RogueClassId);

        Assert.Equal(
            [CampaignRulesetIds.SneakAttackFeatureId],
            rogue.FeaturesByLevel[1]);
    }

    /// The rogue takes the field now, which is what the whole of step 6 was
    /// for — Sneak Attack stops being content nobody carries.
    [Fact]
    public void TheRosterFieldsARogue()
    {
        Assert.Contains(
            CampaignRegistry.Resolve(
                FrontierCampaignIds.CampaignId)
                .Roster
                .Take(CampaignRegistry.Resolve(
                    FrontierCampaignIds.CampaignId)
                    .ActivePartySize),
            character => character.ClassId
                == CampaignRulesetIds.RogueClassId);
    }

    /// End to end on authored content: the ruleset declares the feature, the
    /// class names it, resolving a rogue carries it, and an encounter asks for
    /// the die.
    [Fact]
    public void ARogueResolvedFromTheCampaign_IsAskedForTheSneakAttackDie()
    {
        CharacterSnapshot rogue = ResolveRogue();

        Assert.Equal(
            [CampaignRulesetIds.SneakAttackFeatureId],
            rogue.ClassFeatures);

        Assert.Equal(
            [DieType.D6],
            Evaluate(rogue, allyAdjacentToTarget: true)
                .DamageContributions.RequiredDice);
    }

    /// The same rogue, with nobody crowding the target, is asked for nothing —
    /// the condition is content rather than decoration.
    [Fact]
    public void ARogueWithNoOpening_IsAskedForNothing()
    {
        CombatAttackEvaluation evaluation = Evaluate(
            ResolveRogue(),
            allyAdjacentToTarget: false);

        Assert.NotNull(evaluation.RequiredDamageDice);
        Assert.True(evaluation.DamageContributions.IsEmpty);
    }

    /// Through the staging the client actually calls, so what is proven is the
    /// path rather than a rule in isolation.
    private static CombatAttackEvaluation Evaluate(
        CharacterSnapshot rogue,
        bool allyAdjacentToTarget)
    {
        EncounterState encounter = CreateEncounter(
            rogue,
            allyAdjacentToTarget);

        return CombatAttackStaging.Evaluate(
            encounter,
            encounter.Revision,
            "combatant.rogue",
            "combatant.target",
            SunkenChapelScenarioIds.GuardianDaggerId,
            firstAttackRoll: 15,
            secondAttackRoll: null,
            contributionRolls: Array.Empty<int>());
    }

    private static FeatureDefinition Feature(
        string featureId)
    {
        return Assert.Single(
            RulesetRegistry
                .Resolve(RulesetRegistry.CampaignRulesetId)
                .Definition
                .Features,
            feature => feature.Id == featureId);
    }

    private static CharacterSnapshot ResolveRogue()
    {
        return new CharacterResolver(
            RulesetRegistry.Resolve(RulesetRegistry.CampaignRulesetId))
            .Resolve(new CharacterDraft
            {
                Name = "Test Rogue",
                Level = 1,
                RaceId = "race.human",
                ClassId = CampaignRulesetIds.RogueClassId,
                BackgroundId = "background.soldier",
                AbilityScoreGenerationMethod =
                    AbilityScoreGenerationMethod.Manual,
                BaseAbilityScores = new Dictionary<Ability, int>
                {
                    [Ability.Strength] = 10,
                    [Ability.Dexterity] = 15,
                    [Ability.Constitution] = 13,
                    [Ability.Intelligence] = 12,
                    [Ability.Wisdom] = 11,
                    [Ability.Charisma] = 9
                },
                SelectedSkillIds =
                [
                    "skill.perception",
                    "skill.stealth"
                ],
                EquippedWeaponIds =
                [
                    SunkenChapelScenarioIds.GuardianDaggerId
                ]
            });
    }

    private static EncounterState CreateEncounter(
        CharacterSnapshot rogue,
        bool allyAdjacentToTarget)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipantSetup(
                "combatant.rogue",
                "side.party",
                new GridPosition(1, 1),
                rogue),
            CreateParticipantSetup(
                "combatant.target",
                "side.enemy",
                new GridPosition(2, 1),
                rogue),
            CreateParticipantSetup(
                "combatant.ally",
                "side.party",
                allyAdjacentToTarget
                    ? new GridPosition(3, 1)
                    : new GridPosition(1, 9),
                rogue)
        ];

        return EncounterRules.Start(
            "encounter.rogue",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.rogue",
                Width = 12,
                Height = 12,
                BlockedPositions = Array.Empty<GridPosition>(),
                CoverPositions = Array.Empty<EncounterCoverPosition>(),
                DifficultTerrainPositions = Array.Empty<GridPosition>()
            },
            participants,
            [
                CreateInitiative("combatant.rogue", 1, 20),
                CreateInitiative("combatant.target", 2, 10),
                CreateInitiative("combatant.ally", 3, 5)
            ]);
    }

    private static EncounterParticipantSetup CreateParticipantSetup(
        string combatantId,
        string sideId,
        GridPosition position,
        CharacterSnapshot snapshot)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 20,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = CreateProfile(snapshot),
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    /// The same three fields the party mapper copies across, which is the one
    /// hop this test cannot take: the mapper resolves its character from the
    /// campaign roster, and no rogue is in it yet.
    private static EncounterCombatProfile CreateProfile(
        CharacterSnapshot snapshot)
    {
        return new EncounterCombatProfile
        {
            ArmorClass = snapshot.ArmorClass ?? 10,
            WeaponAttacks = snapshot.WeaponAttacks,
            Contributions = snapshot.Contributions,
            SavingThrowBonuses = snapshot.SavingThrowBonuses
        };
    }

    private static InitiativeOrderEntry CreateInitiative(
        string combatantId,
        int position,
        int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }
}
