using FiveEGoldBox.Core.Internal;

namespace FiveEGoldBox.Core.Rules;

public static class CombatTurnRules
{
    public static CombatTurnState StartCombat(
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        ArgumentNullException.ThrowIfNull(initiativeOrder);

        ValidateInitiativeOrder(initiativeOrder);

        if (initiativeOrder.Count == 0)
        {
            throw new ArgumentException(
                "Initiative order must contain at least one combatant.",
                nameof(initiativeOrder));
        }

        return new CombatTurnState
        {
            InitiativeOrder = CoreCollectionProtection.ProtectList(initiativeOrder),
            RoundNumber = 1,
            ActivePosition = 1
        };
    }

    public static CombatTurnState AdvanceTurn(
        CombatTurnState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        if (state.ActivePosition < state.InitiativeOrder.Count)
        {
            return state with
            {
                ActivePosition = state.ActivePosition + 1
            };
        }

        return state with
        {
            RoundNumber = checked(state.RoundNumber + 1),
            ActivePosition = 1
        };
    }

    public static InitiativeOrderEntry GetActiveCombatant(
        CombatTurnState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ValidateState(state);

        return state.InitiativeOrder[state.ActivePosition - 1];
    }

    private static void ValidateState(
        CombatTurnState state)
    {
        ArgumentNullException.ThrowIfNull(state.InitiativeOrder);

        ValidateInitiativeOrder(state.InitiativeOrder);

        if (state.InitiativeOrder.Count == 0)
        {
            throw new ArgumentException(
                "Initiative order must contain at least one combatant.",
                nameof(state));
        }

        if (state.RoundNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.RoundNumber,
                "Round number must be at least 1.");
        }

        if (state.ActivePosition < 1 || state.ActivePosition > state.InitiativeOrder.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.ActivePosition,
                "Active position must reference an entry in the initiative order.");
        }
    }

    private static void ValidateInitiativeOrder(
        IReadOnlyList<InitiativeOrderEntry> initiativeOrder)
    {
        HashSet<string> combatantIds = new(StringComparer.Ordinal);
        HashSet<int> positions = new();

        foreach (InitiativeOrderEntry entry in initiativeOrder)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (string.IsNullOrWhiteSpace(entry.CombatantId))
            {
                throw new ArgumentException(
                    "Combatant ID is required.",
                    nameof(initiativeOrder));
            }

            ArgumentNullException.ThrowIfNull(entry.Initiative);

            if (!combatantIds.Add(entry.CombatantId))
            {
                throw new ArgumentException(
                    $"Duplicate combatant ID '{entry.CombatantId}' is not allowed.",
                    nameof(initiativeOrder));
            }

            if (!positions.Add(entry.Position))
            {
                throw new ArgumentException(
                    $"Duplicate initiative position '{entry.Position}' is not allowed.",
                    nameof(initiativeOrder));
            }
        }

        for (int expectedPosition = 1; expectedPosition <= initiativeOrder.Count; expectedPosition++)
        {
            if (!positions.Contains(expectedPosition))
            {
                throw new ArgumentException(
                    "Initiative positions must be contiguous and start at 1.",
                    nameof(initiativeOrder));
            }
        }
    }
}
