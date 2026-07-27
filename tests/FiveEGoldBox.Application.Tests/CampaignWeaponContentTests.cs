using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Tests;

/// The gear the baseline party needs, pinned by what each piece is for rather
/// than by its numbers.
public sealed class CampaignWeaponContentTests
{
    /// The first ammunition weapon in the ruleset that no ranger owns. The
    /// post-combat projection has been generic since PR #102 and untestable
    /// since, because the only bow belonged to the only character who could
    /// hold one.
    [Fact]
    public void TheShortbow_IsAnAmmunitionWeaponForSomebodyOtherThanTheRanger()
    {
        WeaponDefinition shortbow = Weapon(
            CampaignRulesetContent.RogueWeaponId);

        Assert.Equal(WeaponAttackKind.Ranged, shortbow.AttackKind);
        Assert.Equal(
            CampaignRulesetContent.RangerAmmunitionItemId,
            shortbow.AmmunitionItemId);
        Assert.Contains(
            RuleIds.WeaponProperties.Ammunition,
            shortbow.Properties);
        Assert.NotEqual(
            CampaignRulesetContent.RangerWeaponId,
            shortbow.Id);
    }

    /// 5e makes the shortbow simple and the longbow martial, which is the
    /// difference that puts one of them in a rogue's hands.
    [Fact]
    public void TheShortbow_IsSimpleWhereTheLongbowIsMartial()
    {
        Assert.Equal(
            WeaponCategory.Simple,
            Weapon(CampaignRulesetContent.RogueWeaponId).Category);
        Assert.Equal(
            WeaponCategory.Martial,
            Weapon(CampaignRulesetContent.RangerWeaponId).Category);
    }

    /// Sneak Attack asks for a finesse or ranged weapon, so a rogue needs one
    /// of each to be dangerous at both distances.
    [Fact]
    public void TheDagger_IsFinesseSoSneakAttackReachesItInMelee()
    {
        Assert.Contains(
            RuleIds.WeaponProperties.Finesse,
            Weapon(CampaignRulesetContent.RogueSidearmWeaponId)
                .Properties);
    }

    [Fact]
    public void TheMace_IsABludgeoningMeleeWeapon()
    {
        WeaponDefinition mace = Weapon(
            CampaignRulesetContent.ClericWeaponId);

        Assert.Equal(WeaponAttackKind.Melee, mace.AttackKind);
        Assert.Equal("damage.bludgeoning", mace.DamageType);
        Assert.Equal(5, mace.ReachFeet);
    }

    /// Phase 7's alignment check exists so a weapon cannot reach the table
    /// with a die nothing can roll. Three new weapons is exactly when it
    /// earns its keep.
    [Fact]
    public void EveryWeaponDieCanBeRolled()
    {
        Assert.All(
            CampaignRulesetContent.CreateRulesetDefinition().Weapons,
            weapon => Assert.True(Enum.IsDefined(weapon.Damage.Die)));
    }

    private static WeaponDefinition Weapon(
        string weaponId)
    {
        return Assert.Single(
            CampaignRulesetContent.CreateRulesetDefinition().Weapons,
            weapon => weapon.Id == weaponId);
    }
}
