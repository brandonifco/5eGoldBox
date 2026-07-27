using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// A prepared spell becoming a spell its caster can actually cast.
///
/// Spellcasting has resolved correctly in Core since PR #134, but only against
/// hand-built combat profiles. Nothing turned a campaign character's prepared
/// spell list into `SpellAttack`s, so a cleric who reached a battlefield
/// reached it with none — which is the half of the design's step 2 that says
/// resources are "projected into an encounter and back out".
public sealed class CampaignCasterProjectionTests
{
    [Fact]
    public void ACleric_ResolvesEveryPreparedSpell()
    {
        CharacterSnapshot cleric = ResolveCaster(
            CampaignRulesetContent.ClericClassId,
            [
                CampaignRulesetContent.SacredFlameId,
                CampaignRulesetContent.CureWoundsId,
                CampaignRulesetContent.BlessId
            ]);

        Assert.Equal(
            [
                CampaignRulesetContent.SacredFlameId,
                CampaignRulesetContent.CureWoundsId,
                CampaignRulesetContent.BlessId
            ],
            cleric.SpellAttacks.Select(spell => spell.SpellId));
    }

    /// A character whose class does not cast gets none of this, however the
    /// draft is filled in.
    [Fact]
    public void AFighter_ResolvesNoSpellsEvenIfItNamesSome()
    {
        Assert.Empty(
            ResolveCaster(
                CampaignRulesetContent.FighterClassId,
                [CampaignRulesetContent.FireBoltId])
                .SpellAttacks);
    }

    /// 5e: attack bonus is the spellcasting modifier plus proficiency, and the
    /// save DC is eight more than that. Wisdom 14 gives +2, and a level-one
    /// character has +2 proficiency.
    [Fact]
    public void ACaster_CarriesItsOwnAttackBonusAndSaveDc()
    {
        SpellAttack sacredFlame = Assert.Single(
            ResolveCaster(
                CampaignRulesetContent.ClericClassId,
                [CampaignRulesetContent.SacredFlameId])
                .SpellAttacks);

        Assert.Equal(4, sacredFlame.AttackBonus);
        Assert.Equal(12, sacredFlame.SaveDc);
        Assert.Equal(Ability.Dexterity, sacredFlame.SaveAbility);
    }

    /// A cantrip spends nothing; a slot spell names the slot it spends, which
    /// is what lets the encounter check a caster can afford it.
    [Fact]
    public void ACantripSpendsNothingAndASlotSpellNamesItsSlot()
    {
        IReadOnlyList<SpellAttack> spells = ResolveCaster(
            CampaignRulesetContent.ClericClassId,
            [
                CampaignRulesetContent.SacredFlameId,
                CampaignRulesetContent.CureWoundsId
            ])
            .SpellAttacks;

        Assert.Null(spells[0].SlotResourceId);
        Assert.Equal("resource.spell-slot.1", spells[1].SlotResourceId);
    }

    /// Cure Wounds adds the caster's modifier and Sacred Flame does not, and
    /// that difference is content rather than something combat works out.
    [Fact]
    public void TheCastersModifierIsFoldedInWhereTheSpellAddsIt()
    {
        IReadOnlyList<SpellAttack> spells = ResolveCaster(
            CampaignRulesetContent.ClericClassId,
            [
                CampaignRulesetContent.SacredFlameId,
                CampaignRulesetContent.CureWoundsId
            ])
            .SpellAttacks;

        Assert.Equal(0, Assert.Single(spells[0].Effects).FlatBonus);
        Assert.Equal(2, Assert.Single(spells[1].Effects).FlatBonus);
    }

    /// Bless installs an effect, and what that effect contributes is resolved
    /// with the spell rather than looked up when it lands.
    [Fact]
    public void ASpellThatInstallsAnEffect_CarriesWhatTheEffectContributes()
    {
        SpellAttack bless = Assert.Single(
            ResolveCaster(
                CampaignRulesetContent.ClericClassId,
                [CampaignRulesetContent.BlessId])
                .SpellAttacks);

        Assert.Equal(
            CampaignRulesetContent.BlessEffectId,
            bless.AppliedEffectId);
        Assert.True(bless.RequiresConcentration);
        Assert.Equal(
            [
                RollContributionTarget.AttackRoll,
                RollContributionTarget.SavingThrow
            ],
            bless.AppliedContributions.Select(
                contribution => contribution.Target));
    }

    /// A wizard casts off Intelligence, so the same machinery gives a
    /// different answer without knowing which class asked.
    [Fact]
    public void AWizard_CastsOffItsOwnAbility()
    {
        SpellAttack fireBolt = Assert.Single(
            ResolveCaster(
                CampaignRulesetContent.WizardClassId,
                [CampaignRulesetContent.FireBoltId])
                .SpellAttacks);

        Assert.Equal(SpellResolutionKind.SpellAttack, fireBolt.Resolution);
        Assert.Equal(DieType.D10, Assert.Single(fireBolt.Effects).Dice.Die);
    }

    /// Wisdom and Intelligence are both 14, so the two casters differ only by
    /// the ability their class casts with rather than by how good they are.
    private static CharacterSnapshot ResolveCaster(
        string classId,
        IReadOnlyList<string> preparedSpellIds)
    {
        ClassDefinition characterClass = Assert.Single(
            CampaignRulesetContent.CreateRulesetDefinition().Classes,
            candidate => candidate.Id == classId);

        return new CharacterResolver(
            CampaignRulesetContent.Load(
                CampaignRulesetContent.CreateRulesetDefinition()))
            .Resolve(new CharacterDraft
            {
                Name = "Test Caster",
                Level = 1,
                RaceId = "race.human",
                ClassId = classId,
                BackgroundId = "background.soldier",
                AbilityScoreGenerationMethod =
                    AbilityScoreGenerationMethod.Manual,
                BaseAbilityScores = new Dictionary<Ability, int>
                {
                    [Ability.Strength] = 12,
                    [Ability.Dexterity] = 12,
                    [Ability.Constitution] = 12,
                    [Ability.Intelligence] = 14,
                    [Ability.Wisdom] = 14,
                    [Ability.Charisma] = 10
                },
                // Each class offers its own skills, so the draft takes
                // whichever ones the class under test actually allows.
                SelectedSkillIds = characterClass.SkillChoices
                    .Take(characterClass.NumberOfSkillChoices)
                    .ToArray(),
                EquippedWeaponIds =
                [
                    CampaignRulesetContent.ClericWeaponId
                ],
                PreparedSpellIds = preparedSpellIds
            });
    }
}
