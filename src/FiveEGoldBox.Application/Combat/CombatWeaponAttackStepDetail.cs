using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// How a weapon attack resolved, from the roll through to the target's
/// resulting condition.
public sealed record CombatWeaponAttackStepDetail
{
    internal CombatWeaponAttackStepDetail(
        string weaponId,
        int distanceFeet,
        bool hasLineOfSight,
        EncounterCoverLevel coverLevel,
        int coverArmorClassBonus,
        D20RollMode attackRollMode,
        int firstRoll,
        int? secondRoll,
        int naturalRoll,
        int attackBonus,
        int attackTotal,
        int targetArmorClass,
        AttackRollOutcome outcome,
        int finalDamage,
        CombatDamagedTargetDetail? damagedTarget)
    {
        WeaponId = weaponId;
        DistanceFeet = distanceFeet;
        HasLineOfSight = hasLineOfSight;
        CoverLevel = coverLevel;
        CoverArmorClassBonus = coverArmorClassBonus;
        AttackRollMode = attackRollMode;
        FirstRoll = firstRoll;
        SecondRoll = secondRoll;
        NaturalRoll = naturalRoll;
        AttackBonus = attackBonus;
        AttackTotal = attackTotal;
        TargetArmorClass = targetArmorClass;
        Outcome = outcome;
        FinalDamage = finalDamage;
        DamagedTarget = damagedTarget;
    }

    public string WeaponId { get; }

    public int DistanceFeet { get; }

    public bool HasLineOfSight { get; }

    public EncounterCoverLevel CoverLevel { get; }

    public int CoverArmorClassBonus { get; }

    public D20RollMode AttackRollMode { get; }

    public int FirstRoll { get; }

    /// Populated only when the roll had advantage or disadvantage.
    public int? SecondRoll { get; }

    public int NaturalRoll { get; }

    public int AttackBonus { get; }

    public int AttackTotal { get; }

    public int TargetArmorClass { get; }

    public AttackRollOutcome Outcome { get; }

    public int FinalDamage { get; }

    /// The target's condition afterwards. Null when the attack dealt no damage.
    public CombatDamagedTargetDetail? DamagedTarget { get; }
}
