using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Scenarios;

/// Class features, which until now were IDs pointing at nothing.
///
/// Sneak Attack is the first, and it is here to answer a question the
/// spellcasting design asked deliberately: whether the roll-contribution seam
/// is secretly spell-shaped. It is not a spell, installs no effect, expires
/// never, and reaches the damage roll rather than the attack — and it needed
/// no new mechanism beyond the conditions it asks for.
internal static partial class CampaignRulesetContent
{
    private static IReadOnlyList<FeatureDefinition> CreateFeatures()
    {
        return
        [
            new FeatureDefinition
            {
                Id = SneakAttackFeatureId,
                Name = "Sneak Attack",
                Contributions =
                [
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.DamageRoll,
                        Dice = new DamageDice
                        {
                            Count = 1,
                            Die = DieType.D6
                        },
                        Conditions =
                        [
                            RollContributionCondition
                                .AdvantageOrAdjacentEnemy,
                            RollContributionCondition
                                .FinesseOrRangedWeapon
                        ]
                    }
                ]
            }
        ];
    }
}
