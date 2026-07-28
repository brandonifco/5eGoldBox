using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Combat;

/// What happened when a concentrating combatant took damage this step.
public sealed record CombatConcentrationCheckStepDetail
{
    internal CombatConcentrationCheckStepDetail(
        string combatantId,
        string effectId,
        bool brokenByIncapacitation,
        int? firstRoll,
        int? combinedSavingThrowBonus,
        int? total,
        int? difficultyClass,
        D20TestOutcome? outcome,
        bool effectDropped)
    {
        CombatantId = combatantId;
        EffectId = effectId;
        BrokenByIncapacitation = brokenByIncapacitation;
        FirstRoll = firstRoll;
        CombinedSavingThrowBonus = combinedSavingThrowBonus;
        Total = total;
        DifficultyClass = difficultyClass;
        Outcome = outcome;
        EffectDropped = effectDropped;
    }

    public string CombatantId { get; }

    public string EffectId { get; }

    /// True when the damage left the combatant no longer conscious —
    /// concentration ends automatically then, so the fields below are null.
    public bool BrokenByIncapacitation { get; }

    public int? FirstRoll { get; }

    public int? CombinedSavingThrowBonus { get; }

    public int? Total { get; }

    public int? DifficultyClass { get; }

    public D20TestOutcome? Outcome { get; }

    public bool EffectDropped { get; }
}
