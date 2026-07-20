using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

internal static class WatchtowerCombatOutcomeTestData
{
    internal const string PartySideId = "side.party";

    internal const string RaiderSideId =
        "side.watchtower-raiders";

    internal const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    internal const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    internal static ApplicationSessionState
        CreatePartyVictorySession(
            int rangerAmmunition = 2)
    {
        ApplicationSessionState source =
            CreateSourceSession();
        PartyMemberState fighter = GetPartyMember(
            source,
            "class.fighter");
        PartyMemberState barbarian = GetPartyMember(
            source,
            "class.barbarian");
        PartyMemberState ranger = GetPartyMember(
            source,
            "class.ranger");

        source = ReplaceParticipantHealth(
            source,
            fighter.PartyMemberId,
            CreateConsciousHealth(
                fighter.Health.HitPoints.MaximumHitPoints,
                currentHitPoints: 7,
                temporaryHitPoints: 3));
        source = ReplaceParticipantHealth(
            source,
            barbarian.PartyMemberId,
            CreateStableHealth(
                barbarian.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            ranger.PartyMemberId,
            CreateFailedSaveDeathHealth(
                ranger.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            MeleeRaiderId,
            CreateDefeatedHealth(
                GetParticipant(source, MeleeRaiderId)
                    .Combatant.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            RangedRaiderId,
            CreateDefeatedHealth(
                GetParticipant(source, RangedRaiderId)
                    .Combatant.Health.HitPoints.MaximumHitPoints));
        source = ReplaceRangerWeapon(
            source,
            weapon => weapon with
            {
                AmmunitionQuantityAvailable = rangerAmmunition
            });

        return Complete(source, PartySideId);
    }

    internal static ApplicationSessionState
        CreateRaiderVictorySession(
            int rangerAmmunition = 1)
    {
        ApplicationSessionState source =
            CreateSourceSession();
        PartyMemberState fighter = GetPartyMember(
            source,
            "class.fighter");
        PartyMemberState barbarian = GetPartyMember(
            source,
            "class.barbarian");
        PartyMemberState ranger = GetPartyMember(
            source,
            "class.ranger");

        source = ReplaceParticipantHealth(
            source,
            fighter.PartyMemberId,
            CreateStableHealth(
                fighter.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            barbarian.PartyMemberId,
            CreateFailedSaveDeathHealth(
                barbarian.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            ranger.PartyMemberId,
            CreateInstantDeathHealth(
                ranger.Health.HitPoints.MaximumHitPoints));
        source = ReplaceParticipantHealth(
            source,
            MeleeRaiderId,
            CreateConsciousHealth(
                GetParticipant(source, MeleeRaiderId)
                    .Combatant.Health.HitPoints.MaximumHitPoints,
                currentHitPoints: 4,
                temporaryHitPoints: 0));
        source = ReplaceParticipantHealth(
            source,
            RangedRaiderId,
            CreateDefeatedHealth(
                GetParticipant(source, RangedRaiderId)
                    .Combatant.Health.HitPoints.MaximumHitPoints));
        source = ReplaceRangerWeapon(
            source,
            weapon => weapon with
            {
                AmmunitionQuantityAvailable = rangerAmmunition
            });

        return Complete(source, RaiderSideId);
    }

    internal static ApplicationSessionState CreateActiveSession()
    {
        return CreateSourceSession();
    }

    internal static PartyMemberState GetPartyMember(
        ApplicationSessionState state,
        string classId)
    {
        return Assert.Single(
            state.Party.Members,
            member => string.Equals(
                member.ClassId,
                classId,
                StringComparison.Ordinal));
    }

    internal static EncounterState GetEncounter(
        ApplicationSessionState state)
    {
        return Assert.IsType<ActiveEncounterState>(
            state.ActiveEncounter).Encounter;
    }

    internal static EncounterParticipantState GetParticipant(
        ApplicationSessionState state,
        string combatantId)
    {
        return Assert.Single(
            GetEncounter(state).Participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    internal static ApplicationSessionState ReplaceParticipant(
        ApplicationSessionState source,
        EncounterParticipantState replacement)
    {
        EncounterState encounter = GetEncounter(source);
        EncounterParticipantState[] participants =
            encounter.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                replacement.Combatant.CombatantId,
                StringComparison.Ordinal));

        Assert.True(index >= 0);
        participants[index] = replacement;

        return ReplaceEncounter(
            source,
            encounter with
            {
                Participants = Array.AsReadOnly(participants)
            });
    }

    internal static ApplicationSessionState ReplaceParticipantHealth(
        ApplicationSessionState source,
        string combatantId,
        CombatantHealthState health)
    {
        EncounterParticipantState participant =
            GetParticipant(source, combatantId);

        return ReplaceParticipant(
            source,
            participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = health
                }
            });
    }

    internal static ApplicationSessionState ReplaceRangerWeapon(
        ApplicationSessionState source,
        Func<WeaponAttack, WeaponAttack> replaceWeapon)
    {
        PartyMemberState ranger = GetPartyMember(
            source,
            "class.ranger");
        EncounterParticipantState participant =
            GetParticipant(source, ranger.PartyMemberId);
        WeaponAttack[] weapons = participant
            .CombatProfile.WeaponAttacks
            .Select(weapon => string.Equals(
                weapon.WeaponId,
                "weapon.longbow",
                StringComparison.Ordinal)
                ? replaceWeapon(weapon)
                : weapon)
            .ToArray();

        return ReplaceParticipant(
            source,
            participant with
            {
                CombatProfile = participant.CombatProfile with
                {
                    WeaponAttacks = Array.AsReadOnly(weapons)
                }
            });
    }

    internal static ApplicationSessionState ReplaceEncounter(
        ApplicationSessionState source,
        EncounterState encounter)
    {
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        return source with
        {
            ActiveEncounter = active with
            {
                Encounter = encounter
            }
        };
    }

    internal static CombatantHealthState CreateConsciousHealth(
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints)
    {
        return new CombatantHealthState
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = maximumHitPoints,
                CurrentHitPoints = currentHitPoints,
                TemporaryHitPoints = temporaryHitPoints
            },
            DeathSavingThrows = new DeathSavingThrowState
            {
                SuccessCount = 0,
                FailureCount = 0,
                IsStable = false
            },
            IsInstantlyDead = false
        };
    }

    internal static CombatantHealthState CreateStableHealth(
        int maximumHitPoints)
    {
        return CreateZeroHealth(
            maximumHitPoints,
            successes: 0,
            failures: 0,
            isStable: true,
            isInstantlyDead: false);
    }

    internal static CombatantHealthState CreateDyingHealth(
        int maximumHitPoints)
    {
        return CreateZeroHealth(
            maximumHitPoints,
            successes: 1,
            failures: 1,
            isStable: false,
            isInstantlyDead: false);
    }

    internal static CombatantHealthState
        CreateFailedSaveDeathHealth(
            int maximumHitPoints)
    {
        return CreateZeroHealth(
            maximumHitPoints,
            successes: 0,
            failures: 3,
            isStable: false,
            isInstantlyDead: false);
    }

    internal static CombatantHealthState CreateInstantDeathHealth(
        int maximumHitPoints)
    {
        return CreateZeroHealth(
            maximumHitPoints,
            successes: 0,
            failures: 0,
            isStable: false,
            isInstantlyDead: true);
    }

    internal static CombatantHealthState CreateDefeatedHealth(
        int maximumHitPoints)
    {
        return CreateZeroHealth(
            maximumHitPoints,
            successes: 0,
            failures: 0,
            isStable: false,
            isInstantlyDead: false);
    }

    private static ApplicationSessionState CreateSourceSession()
    {
        return WatchtowerSignalTestData.CreateEncounterSession()
            with
            {
                RandomValuesConsumed = 37
            };
    }

    private static ApplicationSessionState Complete(
        ApplicationSessionState source,
        string winningSideId)
    {
        EncounterState completed = EncounterRules.Complete(
            GetEncounter(source),
            winningSideId);
        ApplicationSessionState result = ReplaceEncounter(
            source,
            completed);

        ApplicationSessionRules.Validate(result);
        return result;
    }

    private static CombatantHealthState CreateZeroHealth(
        int maximumHitPoints,
        int successes,
        int failures,
        bool isStable,
        bool isInstantlyDead)
    {
        return new CombatantHealthState
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = maximumHitPoints,
                CurrentHitPoints = 0,
                TemporaryHitPoints = 0
            },
            DeathSavingThrows = new DeathSavingThrowState
            {
                SuccessCount = successes,
                FailureCount = failures,
                IsStable = isStable
            },
            IsInstantlyDead = isInstantlyDead
        };
    }
}
