using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public static class WatchtowerCombatOutcomeRules
{
    public static WatchtowerCombatOutcomeResult Finalize(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);
        ApplicationSessionRules.Validate(session);

        ActiveEncounterState activeEncounter =
            session.ActiveEncounter
            ?? throw new ArgumentException(
                "Watchtower combat finalization requires active-encounter state.",
                nameof(session));
        EncounterState encounter = activeEncounter.Encounter;

        if (encounter.LifecycleState
            != EncounterLifecycleState.Completed)
        {
            throw new ArgumentException(
                "Watchtower combat finalization requires a completed encounter.",
                nameof(session));
        }

        if (encounter.Participants.Any(participant =>
            participant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying))
        {
            throw new ArgumentException(
                "A completed watchtower encounter cannot contain a dying participant.",
                nameof(session));
        }

        Dictionary<string, EncounterParticipantState>
            participantsById = CreatePartyParticipantMap(
                session,
                encounter);
        int rangerAmmunition = ResolveRangerAmmunition(
            session,
            participantsById);
        PartyState projectedParty = ProjectParty(
            session.Party,
            participantsById,
            rangerAmmunition);

        bool isPartyVictory = string.Equals(
            encounter.WinningSideId,
            WatchtowerSignalEncounter.PartySideId,
            StringComparison.Ordinal);
        bool isRaiderVictory = string.Equals(
            encounter.WinningSideId,
            WatchtowerSignalEncounter.RaiderSideId,
            StringComparison.Ordinal);

        if (!isPartyVictory && !isRaiderVictory)
        {
            throw new ArgumentException(
                "The completed watchtower encounter has an unsupported winner.",
                nameof(session));
        }

        ApplicationMode resultingMode = isPartyVictory
            ? ApplicationMode.Exploration
            : ApplicationMode.ScenarioConclusion;
        WatchtowerScenarioProgress resultingProgress = isPartyVictory
            ? WatchtowerScenarioProgress.RaidersDefeated
            : WatchtowerScenarioProgress.PartyDefeated;
        WatchtowerCombatOutcome outcome = isPartyVictory
            ? WatchtowerCombatOutcome.PartyVictory
            : WatchtowerCombatOutcome.ScenarioDefeat;

        ApplicationSessionState resultState =
            ApplicationSessionRules.CreateCanonical(
                session with
                {
                    CurrentMode = resultingMode,
                    Party = projectedParty,
                    Scenario = session.Scenario with
                    {
                        Progress = resultingProgress
                    },
                    RegionalTravel = null,
                    Exploration = isPartyVictory
                        ? activeEncounter.ReturnContext
                        : null,
                    ActiveEncounter = null
                });

        return new WatchtowerCombatOutcomeResult
        {
            Outcome = outcome,
            ResultingMode = resultingMode,
            ResultingProgress = resultingProgress,
            State = resultState
        };
    }

    private static Dictionary<string, EncounterParticipantState>
        CreatePartyParticipantMap(
            ApplicationSessionState session,
            EncounterState encounter)
    {
        Dictionary<string, EncounterParticipantState> result =
            new(StringComparer.Ordinal);

        foreach (EncounterParticipantState participant
            in encounter.Participants)
        {
            string participantId =
                participant.Combatant.CombatantId;

            if (!session.Party.Members.Any(member =>
                string.Equals(
                    member.PartyMemberId,
                    participantId,
                    StringComparison.Ordinal)))
            {
                continue;
            }

            if (!string.Equals(
                participant.SideId,
                WatchtowerSignalEncounter.PartySideId,
                StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Party participant '{participantId}' is assigned to the wrong side.",
                    nameof(session));
            }

            if (!result.TryAdd(participantId, participant))
            {
                throw new ArgumentException(
                    $"Party participant '{participantId}' has ambiguous encounter authority.",
                    nameof(session));
            }
        }

        foreach (PartyMemberState member in session.Party.Members)
        {
            if (!result.ContainsKey(member.PartyMemberId))
            {
                throw new ArgumentException(
                    $"Party participant '{member.PartyMemberId}' is missing from the completed encounter.",
                    nameof(session));
            }
        }

        return result;
    }

    private static int ResolveRangerAmmunition(
        ApplicationSessionState session,
        IReadOnlyDictionary<string, EncounterParticipantState>
            participantsById)
    {
        PartyMemberState ranger = AssertSingleRanger(session);
        AmmunitionState persistentAmmunition =
            ranger.Ammunition
            ?? throw new ArgumentException(
                "The bounded Ranger requires persistent ammunition state.",
                nameof(session));

        if (!string.Equals(
            persistentAmmunition.WeaponId,
            WatchtowerPartyDefinitions.RangerWeaponId,
            StringComparison.Ordinal)
            || !string.Equals(
                persistentAmmunition.AmmunitionItemId,
                WatchtowerPartyDefinitions.RangerAmmunitionItemId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The persistent Ranger ammunition identity does not match the authored longbow profile.",
                nameof(session));
        }

        EncounterParticipantState rangerParticipant =
            participantsById[ranger.PartyMemberId];
        ArgumentNullException.ThrowIfNull(
            rangerParticipant.CombatProfile);
        ArgumentNullException.ThrowIfNull(
            rangerParticipant.CombatProfile.WeaponAttacks);

        WeaponAttack[] matchingWeapons =
            rangerParticipant.CombatProfile.WeaponAttacks
                .Where(weapon => string.Equals(
                    weapon.WeaponId,
                    WatchtowerPartyDefinitions.RangerWeaponId,
                    StringComparison.Ordinal))
                .ToArray();

        if (matchingWeapons.Length != 1)
        {
            throw new ArgumentException(
                "The completed Ranger profile must contain exactly one authored longbow.",
                nameof(session));
        }

        WeaponAttack weapon = matchingWeapons[0];

        if (!string.Equals(
            weapon.AmmunitionItemId,
            WatchtowerPartyDefinitions.RangerAmmunitionItemId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The completed Ranger longbow has an unsupported ammunition item.",
                nameof(session));
        }

        int remainingQuantity =
            weapon.AmmunitionQuantityAvailable
            ?? throw new ArgumentException(
                "The completed Ranger longbow requires an authoritative ammunition quantity.",
                nameof(session));

        if (remainingQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(session),
                remainingQuantity,
                "The completed Ranger ammunition quantity must not be negative.");
        }

        return remainingQuantity;
    }

    private static PartyMemberState AssertSingleRanger(
        ApplicationSessionState session)
    {
        PartyMemberState[] rangers = session.Party.Members
            .Where(member => string.Equals(
                member.ClassId,
                WatchtowerPartyDefinitions.RangerClassId,
                StringComparison.Ordinal))
            .ToArray();

        if (rangers.Length != 1)
        {
            throw new ArgumentException(
                "The bounded party must contain exactly one Ranger.",
                nameof(session));
        }

        return rangers[0];
    }

    private static PartyState ProjectParty(
        PartyState source,
        IReadOnlyDictionary<string, EncounterParticipantState>
            participantsById,
        int rangerAmmunition)
    {
        PartyMemberState[] projectedMembers = source.Members
            .Select(member =>
            {
                EncounterParticipantState participant =
                    participantsById[member.PartyMemberId];
                AmmunitionState? ammunition = member.Ammunition;

                if (string.Equals(
                    member.ClassId,
                    WatchtowerPartyDefinitions.RangerClassId,
                    StringComparison.Ordinal))
                {
                    ammunition = ammunition! with
                    {
                        RemainingQuantity = rangerAmmunition
                    };
                }

                return member with
                {
                    Health = participant.Combatant.Health,
                    Ammunition = ammunition
                };
            })
            .ToArray();

        return source with
        {
            Members = Array.AsReadOnly(projectedMembers)
        };
    }
}
