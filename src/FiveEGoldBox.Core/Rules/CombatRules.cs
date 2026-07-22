using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Core.Rules;

public static class CombatRules
{
    public static AttackResolutionResult ResolveAttack(
        D20RollMode rollMode,
        int firstRoll,
        int? secondRoll,
        int attackBonus,
        int targetArmorClass,
        DamageDice damage,
        IReadOnlyList<int> damageRolls,
        int damageBonus,
        IReadOnlyList<DamageResponseType> responseTypes)
    {
        ArgumentNullException.ThrowIfNull(damage);
        ArgumentNullException.ThrowIfNull(damageRolls);
        ArgumentNullException.ThrowIfNull(responseTypes);

        AttackRollResult attackRoll = AttackRollRules.ResolveResult(
            rollMode,
            firstRoll,
            secondRoll,
            attackBonus,
            targetArmorClass);

        AttackDamageResolutionResult damageResolution = DamageRules.ResolveAttackDamage(
            damage,
            attackRoll.Outcome,
            damageRolls,
            damageBonus,
            responseTypes);

        return new AttackResolutionResult
        {
            AttackRoll = attackRoll,
            Damage = damageResolution
        };
    }
}
