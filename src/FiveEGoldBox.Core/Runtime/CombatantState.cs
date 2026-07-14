using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record CombatantState
{
    public required string CombatantId { get; init; }

    public required CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; init; }

    public required CombatantHealthState Health { get; init; }

    public CombatantLifecycleState LifecycleState
    {
        get
        {
            if (Health.IsDead)
            {
                return CombatantLifecycleState.Dead;
            }

            if (!Health.HitPoints.IsAtZeroHitPoints)
            {
                return CombatantLifecycleState.Conscious;
            }

            if (ZeroHitPointPolicy
                == CombatantZeroHitPointPolicy.Defeated)
            {
                return CombatantLifecycleState.Defeated;
            }

            return Health.DeathSavingThrows.IsStable
                ? CombatantLifecycleState.Stable
                : CombatantLifecycleState.Dying;
        }
    }

    public bool IsUnconscious =>
        LifecycleState is CombatantLifecycleState.Dying
            or CombatantLifecycleState.Stable;

    public bool IsTerminal =>
        LifecycleState is CombatantLifecycleState.Dead
            or CombatantLifecycleState.Defeated;
}
