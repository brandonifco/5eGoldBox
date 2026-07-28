using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// How a spell cast resolved. Exactly one of AttackRollMode/SaveAbility is
/// set, matching whichever roll the spell's own resolution needed — or
/// neither, for a spell that simply happens.
public sealed record CombatSpellAttackStepDetail
{
    internal CombatSpellAttackStepDetail(
        string spellId,
        int distanceFeet,
        D20RollMode? attackRollMode,
        int? firstAttackRoll,
        int? secondAttackRoll,
        int? attackNaturalRoll,
        int? attackBonus,
        int? attackTotal,
        int? targetArmorClass,
        AttackRollOutcome? attackOutcome,
        Ability? saveAbility,
        int? saveFirstRoll,
        int? combinedSavingThrowBonus,
        int? saveDifficultyClass,
        D20TestOutcome? saveOutcome,
        bool tookEffect,
        int damageDealt,
        int healingDone,
        CombatDamagedTargetDetail? damagedTarget,
        IReadOnlyList<string>? effectedCombatantIds = null)
    {
        EffectedCombatantIds = effectedCombatantIds is null
            ? Array.Empty<string>()
            : Array.AsReadOnly(effectedCombatantIds.ToArray());
        SpellId = spellId;
        DistanceFeet = distanceFeet;
        AttackRollMode = attackRollMode;
        FirstAttackRoll = firstAttackRoll;
        SecondAttackRoll = secondAttackRoll;
        AttackNaturalRoll = attackNaturalRoll;
        AttackBonus = attackBonus;
        AttackTotal = attackTotal;
        TargetArmorClass = targetArmorClass;
        AttackOutcome = attackOutcome;
        SaveAbility = saveAbility;
        SaveFirstRoll = saveFirstRoll;
        CombinedSavingThrowBonus = combinedSavingThrowBonus;
        SaveDifficultyClass = saveDifficultyClass;
        SaveOutcome = saveOutcome;
        TookEffect = tookEffect;
        DamageDealt = damageDealt;
        HealingDone = healingDone;
        DamagedTarget = damagedTarget;
    }

    public string SpellId { get; }

    public int DistanceFeet { get; }

    /// Set only for a spell resolved by an attack roll.
    public D20RollMode? AttackRollMode { get; }

    public int? FirstAttackRoll { get; }

    public int? SecondAttackRoll { get; }

    public int? AttackNaturalRoll { get; }

    public int? AttackBonus { get; }

    public int? AttackTotal { get; }

    public int? TargetArmorClass { get; }

    public AttackRollOutcome? AttackOutcome { get; }

    /// Set only for a spell resolved by the target's saving throw.
    public Ability? SaveAbility { get; }

    public int? SaveFirstRoll { get; }

    public int? CombinedSavingThrowBonus { get; }

    public int? SaveDifficultyClass { get; }

    public D20TestOutcome? SaveOutcome { get; }

    public bool TookEffect { get; }

    public int DamageDealt { get; }

    public int HealingDone { get; }

    /// The target's condition afterward. Null when the spell dealt no
    /// damage.
    public CombatDamagedTargetDetail? DamagedTarget { get; }

    /// Everyone the spell settled its effect on, in the order it reached
    /// them — the named target first, then every additional one. Empty for
    /// a spell that applies no effect.
    public IReadOnlyList<string> EffectedCombatantIds { get; }
}
