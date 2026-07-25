namespace FiveEGoldBox.Application.Combat;

internal sealed record WatchtowerRaiderAttackPlan
{
    internal required string TargetCombatantId { get; init; }

    internal required string WeaponId { get; init; }
}
