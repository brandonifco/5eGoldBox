using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

/// Checks a session that is in the middle of a fight.
///
/// Everything here is structural — where the fight is happening, who is on the
/// battlefield, which side they are on, whether it has legitimately ended. All
/// of it is read from the encounter the scenario authored, so any adventure's
/// battle can be held in session state. How those combatants decide to act is
/// a separate matter, and is still written per adventure.
internal static class EncounterSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.RegionalTravel is not null)
        {
            throw new ArgumentException(
                "An encounter session cannot contain regional-travel state.",
                nameof(state));
        }

        if (state.Exploration is not null)
        {
            throw new ArgumentException(
                "An encounter session cannot contain root exploration state.",
                nameof(state));
        }

        ActiveEncounterState activeEncounter =
            state.ActiveEncounter
            ?? throw new ArgumentException(
                "An encounter session requires active-encounter state.",
                nameof(state));

        ExplorationState returnContext =
            activeEncounter.ReturnContext
            ?? throw new ArgumentException(
                "An active encounter requires an exploration return context.",
                nameof(state));

        EncounterState encounter =
            activeEncounter.Encounter
            ?? throw new ArgumentException(
                "An active encounter requires Core encounter state.",
                nameof(state));

        ArgumentNullException.ThrowIfNull(encounter.Battlefield);
        ArgumentNullException.ThrowIfNull(encounter.Participants);

        EncounterDefinition definition = ScenarioDefinitionRegistry
            .Resolve(state)
            .Encounters
            .FirstOrDefault(candidate => string.Equals(
                candidate.EncounterId,
                encounter.EncounterId,
                StringComparison.Ordinal))
            ?? throw new ArgumentException(
                "The active encounter is not one this scenario authored.",
                nameof(state));

        // The party must have got here through the trigger that starts this
        // encounter, standing where that trigger fires.
        ScenarioTriggerDefinition trigger =
            ScenarioTriggerMatcher.FindForEncounter(
                state,
                encounter.EncounterId)
            ?? throw new ArgumentException(
                "No trigger in this scenario starts the active encounter.",
                nameof(state));

        if (!string.Equals(
            state.CurrentLocationId,
            trigger.LocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The encounter must be fought where the trigger that started it fires.",
                nameof(state));
        }

        if (!string.Equals(
            state.Scenario.ProgressId,
            trigger.ResultingProgressId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The encounter requires the progress its trigger produces.",
                nameof(state));
        }

        if (!ScenarioTriggerMatcher.MatchesPosition(
            trigger,
            returnContext))
        {
            throw new ArgumentException(
                "The encounter return context must be where its trigger fires.",
                nameof(state));
        }

        ExplorationMapDefinition map =
            ScenarioExplorationMap.FindCurrent(state)
            ?? throw new ArgumentException(
                "The encounter location has no map to return to.",
                nameof(state));

        ScenarioExplorationMap.Validate(map, returnContext);

        ValidateBattlefield(state, definition, encounter);
        ValidateLifecycleShape(definition, encounter);
        ValidateParticipants(state, definition, encounter);
        ValidateCompletedParticipantState(encounter);
    }

    private static void ValidateBattlefield(
        ApplicationSessionState state,
        EncounterDefinition definition,
        EncounterState encounter)
    {
        if (!string.Equals(
            encounter.Battlefield.BattlefieldId,
            definition.BattlefieldId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The active encounter is being fought on the wrong battlefield.",
                nameof(state));
        }
    }

    private static void ValidateLifecycleShape(
        EncounterDefinition definition,
        EncounterState encounter)
    {
        switch (encounter.LifecycleState)
        {
            case EncounterLifecycleState.Active:
                if (encounter.WinningSideId is not null)
                {
                    throw new ArgumentException(
                        "A running encounter cannot have a winning side.",
                        nameof(encounter));
                }

                break;
            case EncounterLifecycleState.Completed:
                if (!IsAuthoredSide(definition, encounter.WinningSideId))
                {
                    throw new ArgumentException(
                        "A completed encounter requires a side the scenario authored.",
                        nameof(encounter));
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(encounter),
                    encounter.LifecycleState,
                    "Unsupported encounter lifecycle state.");
        }
    }

    private static bool IsAuthoredSide(
        EncounterDefinition definition,
        string? sideId)
    {
        return string.Equals(
                sideId,
                definition.PartySideId,
                StringComparison.Ordinal)
            || definition.Combatants.Any(combatant => string.Equals(
                combatant.SideId,
                sideId,
                StringComparison.Ordinal));
    }

    private static void ValidateParticipants(
        ApplicationSessionState state,
        EncounterDefinition definition,
        EncounterState encounter)
    {
        int expected =
            state.Party.Members.Count + definition.Combatants.Count;

        if (encounter.Participants.Count != expected)
        {
            throw new ArgumentException(
                $"The encounter must contain {expected} participants, one for each party member and authored combatant.",
                nameof(state));
        }

        HashSet<string> participantIds = new(StringComparer.Ordinal);

        foreach (EncounterParticipantState participant
            in encounter.Participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            ArgumentNullException.ThrowIfNull(participant.Combatant);

            string combatantId = participant.Combatant.CombatantId;

            if (!participantIds.Add(combatantId))
            {
                throw new ArgumentException(
                    "The encounter contains duplicate participant identities.",
                    nameof(state));
            }

            if (!IsAuthoredParticipantId(definition, state.Party, combatantId))
            {
                throw new ArgumentException(
                    $"Participant '{combatantId}' is neither a party member nor authored by this encounter.",
                    nameof(state));
            }
        }

        foreach (PartyMemberState member in state.Party.Members)
        {
            RequireParticipantOnSide(
                state,
                encounter,
                member.PartyMemberId,
                definition.PartySideId,
                "Party participant");
        }

        foreach (CombatantDefinition combatant in definition.Combatants)
        {
            RequireParticipantOnSide(
                state,
                encounter,
                combatant.CombatantId,
                combatant.SideId,
                "Authored combatant");
        }
    }

    private static bool IsAuthoredParticipantId(
        EncounterDefinition definition,
        PartyState party,
        string combatantId)
    {
        return party.Members.Any(member => string.Equals(
                member.PartyMemberId,
                combatantId,
                StringComparison.Ordinal))
            || definition.Combatants.Any(combatant => string.Equals(
                combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static void RequireParticipantOnSide(
        ApplicationSessionState state,
        EncounterState encounter,
        string combatantId,
        string sideId,
        string subject)
    {
        EncounterParticipantState? participant =
            encounter.Participants.FirstOrDefault(
                candidate => string.Equals(
                    candidate.Combatant.CombatantId,
                    combatantId,
                    StringComparison.Ordinal));

        if (participant is null)
        {
            throw new ArgumentException(
                $"{subject} '{combatantId}' is missing from the encounter.",
                nameof(state));
        }

        if (!string.Equals(
            participant.SideId,
            sideId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"{subject} '{combatantId}' is assigned to the wrong encounter side.",
                nameof(state));
        }
    }

    private static void ValidateCompletedParticipantState(
        EncounterState encounter)
    {
        if (encounter.LifecycleState
            != EncounterLifecycleState.Completed)
        {
            return;
        }

        if (encounter.Participants.Any(participant =>
            participant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying))
        {
            throw new ArgumentException(
                "A completed encounter cannot contain a dying participant.",
                nameof(encounter));
        }
    }
}
