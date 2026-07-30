namespace FiveEGoldBox.Application.Scenarios;

/// Named IDs into the campaign ruleset that other content (the roster, test
/// content, scenario definitions) refers to by name rather than by a bare
/// string literal.
///
/// These used to live on CampaignRulesetContent alongside the C# that built
/// the ruleset itself. That builder is gone -- data/rulesets/campaign/core.json
/// is authoritative now, loaded through RulesetRegistry -- but the IDs
/// themselves are not hardcoded *content*, they are just names for entries
/// the JSON already declares, and callers outside the ruleset still need a
/// way to refer to "the fighter's class" without restating the literal
/// string everywhere.
internal static class CampaignRulesetIds
{
    internal const string FighterClassId = "class.fighter";

    internal const string ClericClassId = "class.cleric";

    internal const string WizardClassId = "class.wizard";

    internal const string RogueClassId = "class.rogue";

    internal const string ClericWeaponId = "weapon.mace";

    internal const string RangerWeaponId = "weapon.longbow";

    internal const string RangerAmmunitionItemId = "item.arrow";

    internal const string RogueWeaponId = "weapon.shortbow";

    internal const string RogueSidearmWeaponId = "weapon.dagger";

    internal const string FireBoltId = "spell.fire-bolt";

    internal const string SacredFlameId = "spell.sacred-flame";

    internal const string CureWoundsId = "spell.cure-wounds";

    internal const string HealingWordId = "spell.healing-word";

    internal const string MagicMissileId = "spell.magic-missile";

    internal const string BlessId = "spell.bless";

    internal const string BlessEffectId = "effect.bless";

    internal const string SneakAttackFeatureId = "feature.sneak_attack";
}
