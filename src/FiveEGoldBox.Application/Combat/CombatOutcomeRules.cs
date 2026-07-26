using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public static class CombatOutcomeRules
{
    public static CombatOutcomeResult Finalize(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);
        ApplicationSessionRules.Validate(session);

        ActiveEncounterState activeEncounter =
            session.ActiveEncounter
            ?? throw new ArgumentException(
                "Combat finalization requires active-encounter state.",
                nameof(session));
        EncounterState encounter = activeEncounter.Encounter;

        if (encounter.LifecycleState
            != EncounterLifecycleState.Completed)
        {
            throw new ArgumentException(
                "Combat finalization requires a completed encounter.",
                nameof(session));
        }

        if (encounter.Participants.Any(participant =>
            participant.Combatant.LifecycleState
                == CombatantLifecycleState.Dying))
        {
            throw new ArgumentException(
                "A completed encounter cannot contain a dying participant.",
                nameof(session));
        }

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(session);
        EncounterDefinition encounterDefinition = scenario.Encounters
            .FirstOrDefault(candidate => string.Equals(
                candidate.EncounterId,
                encounter.EncounterId,
                StringComparison.Ordinal))
            ?? throw new ArgumentException(
                "The completed encounter is not part of this scenario.",
                nameof(session));

        Dictionary<string, EncounterParticipantState>
            participantsById = CreatePartyParticipantMap(
                session,
                encounter,
                encounterDefinition.PartySideId);
        PartyState projectedParty = ProjectParty(
            session.Party,
            participantsById);

        bool isPartyVictory = string.Equals(
            encounter.WinningSideId,
            encounterDefinition.PartySideId,
            StringComparison.Ordinal);
        bool isOpposingVictory = encounterDefinition.Combatants.Any(
            combatant => string.Equals(
                combatant.SideId,
                encounter.WinningSideId,
                StringComparison.Ordinal));

        if (!isPartyVictory && !isOpposingVictory)
        {
            throw new ArgumentException(
                "The completed encounter has an unsupported winner.",
                nameof(session));
        }

        string resultingProgressId = isPartyVictory
            ? encounterDefinition.Outcome.VictoryProgressId
            : encounterDefinition.Outcome.DefeatProgressId;

        // Whether the scenario ends here is decided by its declared
        // conclusions, not by which side happened to win.
        bool concludesScenario = scenario.Progress.Conclusions.Any(
            conclusion => string.Equals(
                conclusion.ProgressId,
                resultingProgressId,
                StringComparison.Ordinal));

        ApplicationMode resultingMode = concludesScenario
            ? ApplicationMode.ScenarioConclusion
            : ApplicationMode.Exploration;
        CombatOutcome outcome = isPartyVictory
            ? CombatOutcome.PartyVictory
            : CombatOutcome.ScenarioDefeat;

        ApplicationSessionState resultState =
            ApplicationSessionRules.CreateCanonical(
                session with
                {
                    CurrentMode = resultingMode,
                    Party = projectedParty,
                    Scenario = new ScenarioState
                    {
                        ProgressId = resultingProgressId
                    },
                    RegionalTravel = null,
                    // The party goes back to where it was exploring unless the
                    // scenario is over.
                    Exploration = concludesScenario
                        ? null
                        : activeEncounter.ReturnContext,
                    ActiveEncounter = null
                });

        return new CombatOutcomeResult
        {
            Outcome = outcome,
            ResultingMode = resultingMode,
            ResultingProgressId = resultingProgressId,
            State = resultState
        };
    }

    private static Dictionary<string, EncounterParticipantState>
        CreatePartyParticipantMap(
            ApplicationSessionState session,
            EncounterState encounter,
            string partySideId)
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
                partySideId,
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

    private static PartyState ProjectParty(
        PartyState source,
        IReadOnlyDictionary<string, EncounterParticipantState>
            participantsById)
    {
        PartyMemberState[] projectedMembers = source.Members
            .Select(member =>
            {
                EncounterParticipantState participant =
                    participantsById[member.PartyMemberId];

                return member with
                {
                    Health = participant.Combatant.Health,
                    Ammunition = ProjectAmmunition(
                        member,
                        participant)
                };
            })
            .ToArray();

        return source with
        {
            Members = Array.AsReadOnly(projectedMembers)
        };
    }

    /// A member who tracks ammunition between encounters carries out of one
    /// however much the weapon it belongs to has left.
    private static AmmunitionState? ProjectAmmunition(
        PartyMemberState member,
        EncounterParticipantState participant)
    {
        AmmunitionState? persistentAmmunition = member.Ammunition;

        if (persistentAmmunition is null)
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(
            participant.CombatProfile);
        ArgumentNullException.ThrowIfNull(
            participant.CombatProfile.WeaponAttacks);

        WeaponAttack[] matchingWeapons =
            participant.CombatProfile.WeaponAttacks
                .Where(weapon => string.Equals(
                    weapon.WeaponId,
                    persistentAmmunition.WeaponId,
                    StringComparison.Ordinal))
                .ToArray();

        if (matchingWeapons.Length != 1)
        {
            throw new ArgumentException(
                $"The completed profile for '{member.PartyMemberId}' must contain exactly one weapon '{persistentAmmunition.WeaponId}'.",
                nameof(participant));
        }

        WeaponAttack weapon = matchingWeapons[0];

        if (!string.Equals(
            weapon.AmmunitionItemId,
            persistentAmmunition.AmmunitionItemId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The completed weapon '{persistentAmmunition.WeaponId}' for '{member.PartyMemberId}' has an unsupported ammunition item.",
                nameof(participant));
        }

        int remainingQuantity =
            weapon.AmmunitionQuantityAvailable
            ?? throw new ArgumentException(
                $"The completed weapon '{persistentAmmunition.WeaponId}' for '{member.PartyMemberId}' requires an authoritative ammunition quantity.",
                nameof(participant));

        if (remainingQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(participant),
                remainingQuantity,
                $"The completed ammunition quantity for '{member.PartyMemberId}' must not be negative.");
        }

        return persistentAmmunition with
        {
            RemainingQuantity = remainingQuantity
        };
    }
}
