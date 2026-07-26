using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

/// Content checks for spells. A spell that contradicts itself — free but
/// costing a slot, resisted by a save it names no ability for, concentrating
/// on something instantaneous — is caught when the ruleset loads rather than
/// when somebody casts it.
public sealed class RulesetValidatorSpellTests
{
    [Fact]
    public void Validate_WithACoherentSpell_ReturnsSuccess()
    {
        Assert.True(Validate(Damaging()).IsValid);
    }

    [Fact]
    public void Validate_WithACantripClaimingALevel_ReturnsError()
    {
        AssertRejects(
            Damaging() with { Cost = SpellCostKind.Cantrip, Level = 1 },
            "ruleset.spells.level.cantrip_not_zero");
    }

    [Fact]
    public void Validate_WithASlotSpellAtLevelZero_ReturnsError()
    {
        AssertRejects(
            Damaging() with { Cost = SpellCostKind.Slot, Level = 0 },
            "ruleset.spells.level.slot_not_positive");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Validate_WithARangedSpellThatStatesNoReach_ReturnsError(
        int? rangeFeet)
    {
        AssertRejects(
            Damaging() with { RangeFeet = rangeFeet },
            "ruleset.spells.range.missing");
    }

    [Fact]
    public void Validate_WithATouchSpellThatStatesAReach_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                RangeKind = SpellRangeKind.Touch,
                RangeFeet = 60
            },
            "ruleset.spells.range.unexpected");
    }

    [Fact]
    public void Validate_WithASavingThrowAndNoAbility_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                Resolution = SpellResolutionKind.SavingThrow
            },
            "ruleset.spells.save.ability_missing");
    }

    [Fact]
    public void Validate_WithAnAbilityButNoSavingThrow_ReturnsError()
    {
        AssertRejects(
            Damaging() with { SaveAbility = Ability.Dexterity },
            "ruleset.spells.save.ability_unexpected");
    }

    /// Halving damage means nothing on a spell that deals none.
    [Fact]
    public void Validate_WhenHalvingDamageItDoesNotDeal_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                Resolution = SpellResolutionKind.SavingThrow,
                SaveAbility = Ability.Dexterity,
                SaveOutcome = SpellSaveOutcome.HalvesDamage,
                Effects = [Healing()]
            },
            "ruleset.spells.save.halves_nothing");
    }

    [Fact]
    public void Validate_WithASpellThatDoesNothing_ReturnsError()
    {
        AssertRejects(
            Damaging() with { Effects = Array.Empty<SpellEffectDefinition>() },
            "ruleset.spells.effects.none");
    }

    [Fact]
    public void Validate_WithAnUndefinedDie_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                Effects =
                [
                    Damage() with
                    {
                        Dice = new DamageDice { Count = 1, Die = (DieType)7 }
                    }
                ]
            },
            "ruleset.spells.effects.die_undefined");
    }

    [Fact]
    public void Validate_WithDamageOfNoType_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                Effects = [Damage() with { DamageType = null }]
            },
            "ruleset.spells.effects.damage_type_required");
    }

    [Fact]
    public void Validate_WithHealingThatNamesADamageType_ReturnsError()
    {
        AssertRejects(
            Damaging() with
            {
                Effects = [Healing() with { DamageType = "damage.fire" }]
            },
            "ruleset.spells.effects.damage_type_unexpected");
    }

    [Fact]
    public void Validate_WhenConcentratingOnSomethingInstantaneous_ReturnsError()
    {
        AssertRejects(
            Damaging() with { RequiresConcentration = true },
            "ruleset.spells.duration.concentration_instantaneous");
    }

    [Fact]
    public void Validate_WithAnEffectThatExpiresImmediately_ReturnsError()
    {
        AssertRejects(
            Damaging() with { AppliedEffectId = "effect.test" },
            "ruleset.spells.duration.effect_instantaneous");
    }

    [Fact]
    public void Validate_WithASpellTargetingNobody_ReturnsError()
    {
        AssertRejects(
            Damaging() with { MaximumTargets = 0 },
            "ruleset.spells.targets.invalid");
    }

    [Fact]
    public void Validate_WithTwoSpellsSharingAnId_ReturnsError()
    {
        ValidationResult result = Validate(Damaging(), Damaging());

        Assert.Contains(
            result.Issues,
            issue => issue.Code == "ruleset.spells.duplicate_id");
    }

    [Fact]
    public void Validate_WithANamelessSpell_ReturnsError()
    {
        AssertRejects(
            Damaging() with { Name = " " },
            "ruleset.spells.name.required");
    }

    private static SpellDefinition Damaging()
    {
        return new SpellDefinition
        {
            Id = "spell.test",
            Name = "Test Spell",
            Cost = SpellCostKind.Cantrip,
            Level = 0,
            CastingTime = SpellCastingTime.Action,
            RangeKind = SpellRangeKind.Ranged,
            RangeFeet = 60,
            Targets = SpellTargetDisposition.Enemies,
            Resolution = SpellResolutionKind.SpellAttack,
            Effects = [Damage()]
        };
    }

    private static SpellEffectDefinition Damage()
    {
        return new SpellEffectDefinition
        {
            Kind = SpellEffectKind.Damage,
            Dice = new DamageDice { Count = 1, Die = DieType.D8 },
            DamageType = "damage.fire"
        };
    }

    private static SpellEffectDefinition Healing()
    {
        return new SpellEffectDefinition
        {
            Kind = SpellEffectKind.Healing,
            Dice = new DamageDice { Count = 1, Die = DieType.D8 }
        };
    }

    private static void AssertRejects(
        SpellDefinition spell,
        string expectedCode)
    {
        ValidationResult result = Validate(spell);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == expectedCode);
    }

    private static ValidationResult Validate(
        params SpellDefinition[] spells)
    {
        return RulesetValidator.Validate(new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Spells = spells
        });
    }
}
