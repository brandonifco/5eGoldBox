using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatantView
{
    internal CombatantView(
        string combatantId,
        string displayName,
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
        bool hasReactionAvailable,
        string? monsterId = null)
    {
        if (string.IsNullOrWhiteSpace(combatantId))
        {
            throw new ArgumentException(
                "Combatant ID is required.",
                nameof(combatantId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "Display name is required.",
                nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(sideId))
        {
            throw new ArgumentException(
                "Side ID is required.",
                nameof(sideId));
        }

        ArgumentNullException.ThrowIfNull(health);

        CombatantId = combatantId;
        DisplayName = displayName;
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
        MonsterId = monsterId;
    }

    public string CombatantId { get; }

    /// The scenario's shared monster type ID (e.g. "monster.mill-rat"),
    /// distinct from CombatantId's own per-placement instance ID (e.g.
    /// "combatant.mill-rat.first") -- two rats from the same monster share
    /// this value but not CombatantId. Null for a party member, which has
    /// no monster type at all.
    public string? MonsterId { get; }

    /// A party member's real name (PartyMemberState.DisplayName), or --
    /// for opposition placed by the scenario -- the ruleset's
    /// MonsterDefinition.Name, reached only through the internal
    /// EncounterCombatantDefinition placement and RulesetRegistry lookup a
    /// client cannot perform itself. This is the one place a client gets a
    /// real name for every combatant without falling back to the raw
    /// combatant ID.
    public string DisplayName { get; }

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
