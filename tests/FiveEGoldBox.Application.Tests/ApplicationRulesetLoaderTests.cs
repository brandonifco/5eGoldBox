using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// Core validates that a ruleset is well-formed; it cannot validate that this
/// layer can roll the dice in it, because Core does not know this layer exists.
/// These cover the second check.
public sealed class ApplicationRulesetLoaderTests
{
    [Fact]
    public void Load_WithEveryDieCoreDefines_Succeeds()
    {
        RulesetLoadResult result = ApplicationRulesetLoader.Load(
            CreateRuleset(Enum.GetValues<DieType>()));

        Assert.True(result.IsValid);
        Assert.NotNull(result.Ruleset);
    }

    /// The case this check exists for: a die Core accepts but the random
    /// sequence cannot produce is refused at load rather than when it is
    /// rolled. No such die exists today, so it is simulated with a value
    /// outside the enum, which reaches the capability check the same way a
    /// future die would.
    [Fact]
    public void Load_WithDieTheSequenceCannotRoll_Fails()
    {
        RulesetLoadResult result = ApplicationRulesetLoader.Load(
            CreateRuleset([(DieType)7]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Validation.Issues,
            issue => issue.Severity == ValidationSeverity.Error);
    }

    /// Core's own rejection fires first, and the loader passes it through
    /// rather than replacing it.
    [Fact]
    public void Load_WithUndefinedDie_ReportsCoreValidationFailure()
    {
        RulesetLoadResult result = ApplicationRulesetLoader.Load(
            CreateRuleset([(DieType)7]));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Validation.Issues,
            issue => issue.Code == "ruleset.weapons.damage_die.undefined");
    }

    [Fact]
    public void Load_WithNullDefinition_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationRulesetLoader.Load(null!));
    }

    private static RulesetDefinition CreateRuleset(
        IReadOnlyList<DieType> weaponDice)
    {
        return new RulesetDefinition
        {
            Id = "ruleset.die-capability",
            Name = "Die Capability Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    BaseSpeedFeet = 30
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
            ],
            Backgrounds =
            [
                new BackgroundDefinition
                {
                    Id = "background.soldier",
                    Name = "Soldier"
                }
            ],
            Skills = [],
            Weapons = weaponDice
                .Select((die, index) => new WeaponDefinition
                {
                    Id = $"weapon.test-{index}",
                    Name = $"Test Weapon {index}",
                    Category = WeaponCategory.Simple,
                    AttackKind = WeaponAttackKind.Melee,
                    Damage = new DamageDice
                    {
                        Count = 1,
                        Die = die
                    },
                    DamageType = "damage.slashing"
                })
                .ToArray(),
            EquipmentItems = []
        };
    }
}
