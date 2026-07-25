using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// How a death saving throw resolved and where it leaves the combatant.
public sealed record CombatDeathSavingThrowStepDetail
{
    internal CombatDeathSavingThrowStepDetail(
        CombatantLifecycleState previousLifecycleState,
        CombatantLifecycleState lifecycleState,
        int firstRoll,
        int? secondRoll,
        int naturalRoll,
        int savingThrowBonus,
        int total,
        int difficultyClass,
        DeathSavingThrowOutcome outcome,
        int successCount,
        int failureCount,
        bool isStable)
    {
        PreviousLifecycleState = previousLifecycleState;
        LifecycleState = lifecycleState;
        FirstRoll = firstRoll;
        SecondRoll = secondRoll;
        NaturalRoll = naturalRoll;
        SavingThrowBonus = savingThrowBonus;
        Total = total;
        DifficultyClass = difficultyClass;
        Outcome = outcome;
        SuccessCount = successCount;
        FailureCount = failureCount;
        IsStable = isStable;
    }

    public CombatantLifecycleState PreviousLifecycleState { get; }

    public CombatantLifecycleState LifecycleState { get; }

    public int FirstRoll { get; }

    public int? SecondRoll { get; }

    public int NaturalRoll { get; }

    public int SavingThrowBonus { get; }

    public int Total { get; }

    public int DifficultyClass { get; }

    public DeathSavingThrowOutcome Outcome { get; }

    public int SuccessCount { get; }

    public int FailureCount { get; }

    public bool IsStable { get; }
}
