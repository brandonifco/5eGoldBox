using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatantView
{
    internal CombatantView(
        string combatantId,
        string sideId,
        GridPosition position,
        CombatantLifecycleState lifecycleState,
        CombatantHealthState health,
        int armorClass,
        int movementSpeedFeet,
        int movementSpentFeet,
        int movementRemainingFeet,
        bool hasActionAvailable,
        bool hasBonusActionAvailable,
        bool hasReactionAvailable)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
        {
            throw new ArgumentException(
                "Combatant ID is required.",
                nameof(combatantId));
        }

        if (string.IsNullOrWhiteSpace(sideId))
        {
            throw new ArgumentException(
                "Side ID is required.",
                nameof(sideId));
        }

        ArgumentNullException.ThrowIfNull(health);

        CombatantId = combatantId;
        SideId = sideId;
        Position = position;
        LifecycleState = lifecycleState;
        Health = health;
        ArmorClass = armorClass;
        MovementSpeedFeet = movementSpeedFeet;
        MovementSpentFeet = movementSpentFeet;
        MovementRemainingFeet = movementRemainingFeet;
        HasActionAvailable = hasActionAvailable;
        HasBonusActionAvailable = hasBonusActionAvailable;
        HasReactionAvailable = hasReactionAvailable;
    }

    public string CombatantId { get; }

    public string SideId { get; }

    public GridPosition Position { get; }

    public CombatantLifecycleState LifecycleState { get; }

    public CombatantHealthState Health { get; }

    public int ArmorClass { get; }

    public int MovementSpeedFeet { get; }

    public int MovementSpentFeet { get; }

    public int MovementRemainingFeet { get; }

    public bool HasActionAvailable { get; }

    public bool HasBonusActionAvailable { get; }

    public bool HasReactionAvailable { get; }
}
