namespace FiveEGoldBox.Core.Runtime;

internal static class EncounterCompletionRules
{
    internal static EncounterState Resolve(
        EncounterState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.LifecycleState
            != EncounterLifecycleState.Active)
        {
            return state;
        }

        ArgumentNullException.ThrowIfNull(
            state.Participants);

        if (state.Participants.Any(participant =>
            participant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying))
        {
            return state;
        }

        string[] viableSideIds =
            GetViableSideIds(state);

        if (viableSideIds.Length > 1)
        {
            return state;
        }

        return state with
        {
            LifecycleState =
                EncounterLifecycleState.Completed,
            WinningSideId =
                viableSideIds.SingleOrDefault(),
            PendingDeathSavingThrowCombatantId = null
        };
    }

    internal static string[] GetViableSideIds(
        EncounterState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(
            state.Participants);

        return state.Participants
            .Where(participant =>
                participant.Combatant.LifecycleState
                    is CombatantLifecycleState.Conscious
                    or CombatantLifecycleState.Dying)
            .Select(participant => participant.SideId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(
                sideId => sideId,
                StringComparer.Ordinal)
            .ToArray();
    }
}
