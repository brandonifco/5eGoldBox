using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

/// The Disengage action: spend your Action, and your movement stops
/// provoking opportunity attacks for the rest of your turn.
///
/// It exists because opportunity attacks do. Without an answer to them,
/// leaving a melee is a free hit for the enemy with no counterplay, which
/// turns every engagement into a commitment rather than a decision — the
/// opposite of what making position matter is for.
public static class EncounterDisengageRules
{
    public static EncounterDisengageResult Resolve(
        EncounterState state,
        EncounterDisengageCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        EncounterRules.ValidateState(state);
        ValidateCommand(command);

        if (state.LifecycleState != EncounterLifecycleState.Active)
        {
            throw new InvalidOperationException(
                "A completed encounter cannot resolve a disengage.");
        }

        if (command.ExpectedRevision != state.Revision)
        {
            throw new InvalidOperationException(
                $"Expected encounter revision '{command.ExpectedRevision}', but the current revision is '{state.Revision}'.");
        }

        int actorIndex = FindParticipantIndex(
            state,
            command.ActorCombatantId);

        if (actorIndex < 0)
        {
            throw new ArgumentException(
                $"Actor '{command.ActorCombatantId}' is not an encounter participant.",
                nameof(command));
        }

        EncounterParticipantState actor = state.Participants[actorIndex];

        if (!string.Equals(
            actor.Combatant.CombatantId,
            state.ActiveCombatantId,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Only the active combatant can disengage.");
        }

        if (actor.Combatant.LifecycleState
            != CombatantLifecycleState.Conscious)
        {
            throw new InvalidOperationException(
                "The disengaging combatant must be conscious.");
        }

        if (!actor.TurnResources.HasActionAvailable)
        {
            throw new InvalidOperationException(
                "The disengaging combatant has already spent its action.");
        }

        if (actor.TurnResources.HasDisengaged)
        {
            throw new InvalidOperationException(
                "The combatant has already disengaged this turn.");
        }

        EncounterParticipantState[] participants =
            state.Participants.ToArray();

        participants[actorIndex] = actor with
        {
            TurnResources = CombatTurnResourceRules.Disengage(
                actor.TurnResources)
        };

        EncounterState resolvedState = state with
        {
            Revision = checked(state.Revision + 1),
            Participants = Array.AsReadOnly(participants)
        };

        EncounterRules.ValidateState(resolvedState);

        return new EncounterDisengageResult
        {
            ActorCombatantId = command.ActorCombatantId,
            State = resolvedState
        };
    }

    /// Legal-and-available, for a caller building a menu — the same
    /// "availability is a question, illegality is an exception" split
    /// every other rule in this layer already follows.
    public static bool CanDisengage(
        EncounterState state,
        string actorCombatantId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(actorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(actorCombatantId));
        }

        if (state.LifecycleState != EncounterLifecycleState.Active)
        {
            return false;
        }

        int actorIndex = FindParticipantIndex(state, actorCombatantId);

        if (actorIndex < 0)
        {
            return false;
        }

        EncounterParticipantState actor = state.Participants[actorIndex];

        return string.Equals(
                actor.Combatant.CombatantId,
                state.ActiveCombatantId,
                StringComparison.Ordinal)
            && actor.Combatant.LifecycleState
                == CombatantLifecycleState.Conscious
            && actor.TurnResources.HasActionAvailable
            && !actor.TurnResources.HasDisengaged;
    }

    private static void ValidateCommand(
        EncounterDisengageCommand command)
    {
        if (command.ExpectedRevision < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.ExpectedRevision,
                "Expected encounter revision must be at least 1.");
        }

        if (string.IsNullOrWhiteSpace(command.ActorCombatantId))
        {
            throw new ArgumentException(
                "Actor combatant ID is required.",
                nameof(command));
        }
    }

    private static int FindParticipantIndex(
        EncounterState state,
        string combatantId)
    {
        for (int index = 0; index < state.Participants.Count; index++)
        {
            if (string.Equals(
                state.Participants[index].Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}
