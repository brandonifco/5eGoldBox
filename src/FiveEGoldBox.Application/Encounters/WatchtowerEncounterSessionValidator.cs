using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

internal static class WatchtowerEncounterSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        ActiveEncounterState activeEncounter =
            state.ActiveEncounter
            ?? throw new ArgumentException(
                "An encounter session requires active-encounter state.",
                nameof(state));

        if (!string.Equals(
            state.CurrentLocationId,
            WatchtowerRegionalRoute.WatchtowerLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The watchtower ambush requires the ruined-watchtower location.",
                nameof(state));
        }

        if (state.Scenario.Progress
            != WatchtowerScenarioProgress.SignalActivated)
        {
            throw new ArgumentException(
                "The watchtower ambush requires the activated signal.",
                nameof(state));
        }

        ExplorationState returnContext =
            activeEncounter.ReturnContext
            ?? throw new ArgumentException(
                "An active encounter requires an exploration return context.",
                nameof(state));

        WatchtowerExplorationMap.Validate(returnContext);

        if (!WatchtowerSignalMechanism.CanActivate(
            returnContext))
        {
            throw new ArgumentException(
                "The encounter return context must be the authored signal-mechanism state.",
                nameof(state));
        }

        EncounterState encounter =
            activeEncounter.Encounter
            ?? throw new ArgumentException(
                "An active encounter requires Core encounter state.",
                nameof(state));

        ValidateEncounterIdentity(
            state,
            encounter);
        ValidateLifecycleShape(encounter);
        ValidateParticipants(
            state,
            encounter);
        ValidateCompletedParticipantState(encounter);
    }

    private static void ValidateEncounterIdentity(
        ApplicationSessionState state,
        EncounterState encounter)
    {
        if (!string.Equals(
            encounter.EncounterId,
            WatchtowerSignalEncounter.EncounterId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The active encounter ID is unsupported.",
                nameof(state));
        }

        ArgumentNullException.ThrowIfNull(
            encounter.Battlefield);
        ArgumentNullException.ThrowIfNull(
            encounter.Participants);

        if (!string.Equals(
            encounter.Battlefield.BattlefieldId,
            WatchtowerSignalEncounter.BattlefieldId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The active encounter battlefield is unsupported.",
                nameof(state));
        }
    }

    private static void ValidateLifecycleShape(
        EncounterState encounter)
    {
        switch (encounter.LifecycleState)
        {
            case EncounterLifecycleState.Active:
                if (encounter.WinningSideId is not null)
                {
                    throw new ArgumentException(
                        "A running watchtower encounter cannot have a winning side.",
                        nameof(encounter));
                }

                break;
            case EncounterLifecycleState.Completed:
                if (!string.Equals(
                        encounter.WinningSideId,
                        WatchtowerSignalEncounter.PartySideId,
                        StringComparison.Ordinal)
                    && !string.Equals(
                        encounter.WinningSideId,
                        WatchtowerSignalEncounter.RaiderSideId,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "A completed watchtower encounter requires an authored winning side.",
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

    private static void ValidateParticipants(
        ApplicationSessionState state,
        EncounterState encounter)
    {
        if (encounter.Participants.Count != 5)
        {
            throw new ArgumentException(
                "The watchtower ambush must contain exactly five participants.",
                nameof(state));
        }

        HashSet<string> participantIds =
            new(StringComparer.Ordinal);

        foreach (EncounterParticipantState participant
            in encounter.Participants)
        {
            ArgumentNullException.ThrowIfNull(participant);
            ArgumentNullException.ThrowIfNull(
                participant.Combatant);

            string combatantId =
                participant.Combatant.CombatantId;

            if (!participantIds.Add(combatantId))
            {
                throw new ArgumentException(
                    "The watchtower ambush contains duplicate participant identities.",
                    nameof(state));
            }

            if (!WatchtowerSignalEncounter
                .IsAuthoredParticipantId(
                    combatantId,
                    state.Party))
            {
                throw new ArgumentException(
                    $"Participant '{combatantId}' is not part of the authored watchtower ambush.",
                    nameof(state));
            }
        }

        foreach (PartyMemberState member
            in state.Party.Members)
        {
            EncounterParticipantState? participant =
                encounter.Participants.FirstOrDefault(
                    candidate => string.Equals(
                        candidate.Combatant.CombatantId,
                        member.PartyMemberId,
                        StringComparison.Ordinal));

            if (participant is null)
            {
                throw new ArgumentException(
                    $"Party participant '{member.PartyMemberId}' is missing from the watchtower ambush.",
                    nameof(state));
            }

            if (!string.Equals(
                participant.SideId,
                WatchtowerSignalEncounter.PartySideId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Party participant '{member.PartyMemberId}' is assigned to the wrong encounter side.",
                    nameof(state));
            }
        }

        ValidateRaiderParticipant(
            encounter,
            WatchtowerSignalEncounter.MeleeRaiderId);
        ValidateRaiderParticipant(
            encounter,
            WatchtowerSignalEncounter.RangedRaiderId);
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
                "A completed watchtower encounter cannot contain a dying participant.",
                nameof(encounter));
        }
    }

    private static void ValidateRaiderParticipant(
        EncounterState encounter,
        string combatantId)
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
                $"Raider participant '{combatantId}' is missing from the watchtower ambush.",
                nameof(encounter));
        }

        if (!string.Equals(
            participant.SideId,
            WatchtowerSignalEncounter.RaiderSideId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Raider participant '{combatantId}' is assigned to the wrong encounter side.",
                nameof(encounter));
        }
    }
}
