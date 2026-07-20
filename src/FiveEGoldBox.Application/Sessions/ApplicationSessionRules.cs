using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Sessions;

public static class ApplicationSessionRules
{
    private const string FighterClassId = "class.fighter";

    private const string BarbarianClassId = "class.barbarian";

    private const string RangerClassId = "class.ranger";

    private const int RequiredPartyMemberCount = 3;

    public static ApplicationSessionState CreateNew(
        string scenarioId,
        string currentLocationId,
        PartyState party,
        int randomSeed)
    {
        ApplicationSessionState state = new()
        {
            ScenarioId = scenarioId,
            CurrentMode = ApplicationMode.Outpost,
            CurrentLocationId = currentLocationId,
            Party = party,
            Scenario = new WatchtowerScenarioState
            {
                Progress =
                    WatchtowerScenarioProgress
                        .MissionNotAccepted
            },
            RandomSeed = randomSeed,
            RandomValuesConsumed = 0
        };

        return CreateCanonical(state);
    }

    public static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(state.ScenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(state));
        }

        if (!Enum.IsDefined(state.CurrentMode))
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.CurrentMode,
                "Unsupported application mode.");
        }

        if (string.IsNullOrWhiteSpace(
            state.CurrentLocationId))
        {
            throw new ArgumentException(
                "Current location ID is required.",
                nameof(state));
        }

        ValidateParty(state.Party);
        ValidateScenario(state.Scenario);
        ValidateModeState(state);

        if (state.RandomValuesConsumed < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.RandomValuesConsumed,
                "Random values consumed must not be negative.");
        }
    }

    internal static ApplicationSessionState CreateCanonical(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(state.Party);
        ArgumentNullException.ThrowIfNull(
            state.Party.Members);

        PartyMemberState[] members =
            state.Party.Members.ToArray();

        ApplicationSessionState canonicalState =
            state with
            {
                Party = state.Party with
                {
                    Members = Array.AsReadOnly(members)
                }
            };

        Validate(canonicalState);

        return canonicalState;
    }

    private static void ValidateModeState(
        ApplicationSessionState state)
    {
        switch (state.CurrentMode)
        {
            case ApplicationMode.Outpost:
                if (state.RegionalTravel is not null)
                {
                    throw new ArgumentException(
                        "An outpost session cannot contain regional-travel state.",
                        nameof(state));
                }

                if (state.Exploration is not null)
                {
                    throw new ArgumentException(
                        "An outpost session cannot contain exploration state.",
                        nameof(state));
                }

                if (state.ActiveEncounter is not null)
                {
                    throw new ArgumentException(
                        "An outpost session cannot contain an active encounter.",
                        nameof(state));
                }

                break;
            case ApplicationMode.RegionalTravel:
                if (state.Exploration is not null)
                {
                    throw new ArgumentException(
                        "A regional-travel session cannot contain exploration state.",
                        nameof(state));
                }

                if (state.ActiveEncounter is not null)
                {
                    throw new ArgumentException(
                        "A regional-travel session cannot contain an active encounter.",
                        nameof(state));
                }

                ValidateRegionalTravel(state);
                break;
            case ApplicationMode.Exploration:
                if (state.RegionalTravel is not null)
                {
                    throw new ArgumentException(
                        "An exploration session cannot contain regional-travel state.",
                        nameof(state));
                }

                if (state.ActiveEncounter is not null)
                {
                    throw new ArgumentException(
                        "An exploration session cannot contain an active encounter.",
                        nameof(state));
                }

                ValidateExploration(state);
                break;
            case ApplicationMode.Encounter:
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

                WatchtowerEncounterSessionValidator.Validate(state);
                break;
            default:
                throw new ArgumentException(
                    "Only outpost, regional-travel, exploration, and encounter sessions are supported in this application phase.",
                    nameof(state));
        }
    }

    private static void ValidateExploration(
        ApplicationSessionState state)
    {
        ExplorationState exploration =
            state.Exploration
            ?? throw new ArgumentException(
                "An exploration session requires exploration state.",
                nameof(state));

        if (!string.Equals(
            state.CurrentLocationId,
            WatchtowerRegionalRoute.WatchtowerLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Watchtower exploration requires the ruined-watchtower location.",
                nameof(state));
        }

        if (state.Scenario.Progress
            != WatchtowerScenarioProgress.MissionAccepted)
        {
            throw new ArgumentException(
                "Watchtower exploration requires the accepted mission to remain active.",
                nameof(state));
        }

        WatchtowerExplorationMap.Validate(exploration);
    }

    private static void ValidateRegionalTravel(
        ApplicationSessionState state)
    {
        RegionalTravelState travel =
            state.RegionalTravel
            ?? throw new ArgumentException(
                "A regional-travel session requires regional-travel state.",
                nameof(state));

        if (state.Scenario.Progress
            == WatchtowerScenarioProgress.MissionNotAccepted)
        {
            throw new ArgumentException(
                "Regional travel cannot begin before the watchtower mission is accepted.",
                nameof(state));
        }

        if (!string.Equals(
            travel.RouteId,
            WatchtowerRegionalRoute.RouteId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The regional-travel route is unsupported.",
                nameof(state));
        }

        if (!WatchtowerRegionalRoute.HasSupportedEndpoints(
            travel.OriginLocationId,
            travel.DestinationLocationId))
        {
            throw new ArgumentException(
                "The regional-travel endpoints are inconsistent with the watchtower route.",
                nameof(state));
        }

        if (travel.FinalStepIndex
            != WatchtowerRegionalRoute.FinalStepIndex)
        {
            throw new ArgumentException(
                "The regional-travel final step is inconsistent with the watchtower route.",
                nameof(state));
        }

        if (travel.CurrentStepIndex < 0
            || travel.CurrentStepIndex
                > travel.FinalStepIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                travel.CurrentStepIndex,
                "The regional-travel current step must be within the fixed route.");
        }

        string expectedLocationId = travel.IsComplete
            ? travel.DestinationLocationId
            : travel.OriginLocationId;

        if (!string.Equals(
            state.CurrentLocationId,
            expectedLocationId,
            StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The current location is inconsistent with regional-travel progress.",
                nameof(state));
        }
    }

    private static void ValidateParty(
        PartyState party)
    {
        ArgumentNullException.ThrowIfNull(party);

        if (string.IsNullOrWhiteSpace(party.PartyId))
        {
            throw new ArgumentException(
                "Party ID is required.",
                nameof(party));
        }

        ArgumentNullException.ThrowIfNull(party.Members);

        if (party.Members.Count
            != RequiredPartyMemberCount)
        {
            throw new ArgumentException(
                $"The bounded party must contain exactly {RequiredPartyMemberCount} members.",
                nameof(party));
        }

        HashSet<string> partyMemberIds =
            new(StringComparer.Ordinal);
        HashSet<string> characterDefinitionIds =
            new(StringComparer.Ordinal);

        int fighterCount = 0;
        int barbarianCount = 0;
        int rangerCount = 0;

        foreach (PartyMemberState member in party.Members)
        {
            ArgumentNullException.ThrowIfNull(member);

            ValidatePartyMember(member);

            if (!partyMemberIds.Add(
                member.PartyMemberId))
            {
                throw new ArgumentException(
                    "Party-member IDs must be unique.",
                    nameof(party));
            }

            if (!characterDefinitionIds.Add(
                member.CharacterDefinitionId))
            {
                throw new ArgumentException(
                    "Character-definition IDs must be unique.",
                    nameof(party));
            }

            switch (member.ClassId)
            {
                case FighterClassId:
                    fighterCount++;
                    break;
                case BarbarianClassId:
                    barbarianCount++;
                    break;
                case RangerClassId:
                    rangerCount++;
                    break;
                default:
                    throw new ArgumentException(
                        "The bounded party supports only Fighter, Barbarian, and Ranger class IDs.",
                        nameof(party));
            }
        }

        if (fighterCount != 1
            || barbarianCount != 1
            || rangerCount != 1)
        {
            throw new ArgumentException(
                "The bounded party must contain one Fighter, one Barbarian, and one Ranger.",
                nameof(party));
        }
    }

    private static void ValidatePartyMember(
        PartyMemberState member)
    {
        if (string.IsNullOrWhiteSpace(
            member.PartyMemberId))
        {
            throw new ArgumentException(
                "Party-member ID is required.",
                nameof(member));
        }

        if (string.IsNullOrWhiteSpace(
            member.CharacterDefinitionId))
        {
            throw new ArgumentException(
                "Character-definition ID is required.",
                nameof(member));
        }

        if (string.IsNullOrWhiteSpace(
            member.DisplayName))
        {
            throw new ArgumentException(
                "Party-member display name is required.",
                nameof(member));
        }

        if (string.IsNullOrWhiteSpace(member.ClassId))
        {
            throw new ArgumentException(
                "Class ID is required.",
                nameof(member));
        }

        if (!Enum.IsDefined(
            member.ZeroHitPointPolicy))
        {
            throw new ArgumentOutOfRangeException(
                nameof(member),
                member.ZeroHitPointPolicy,
                "Unsupported zero-hit-point policy.");
        }

        ValidateHealth(
            member.Health,
            member.ZeroHitPointPolicy);

        if (member.ClassId == RangerClassId)
        {
            if (member.Ammunition is null)
            {
                throw new ArgumentException(
                    "The Ranger must have ammunition state.",
                    nameof(member));
            }

            ValidateAmmunition(member.Ammunition);
        }
        else if (member.Ammunition is not null)
        {
            throw new ArgumentException(
                "Only the Ranger may have ammunition state in the bounded party.",
                nameof(member));
        }
    }

    private static void ValidateHealth(
        CombatantHealthState health,
        CombatantZeroHitPointPolicy
            zeroHitPointPolicy)
    {
        ArgumentNullException.ThrowIfNull(health);
        ArgumentNullException.ThrowIfNull(
            health.HitPoints);
        ArgumentNullException.ThrowIfNull(
            health.DeathSavingThrows);

        HitPointState hitPoints = health.HitPoints;
        DeathSavingThrowState deathSavingThrows =
            health.DeathSavingThrows;

        if (hitPoints.MaximumHitPoints <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                hitPoints.MaximumHitPoints,
                "Maximum hit points must be greater than 0.");
        }

        if (hitPoints.CurrentHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                hitPoints.CurrentHitPoints,
                "Current hit points must not be negative.");
        }

        if (hitPoints.CurrentHitPoints
            > hitPoints.MaximumHitPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                hitPoints.CurrentHitPoints,
                "Current hit points must not exceed maximum hit points.");
        }

        if (hitPoints.TemporaryHitPoints < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                hitPoints.TemporaryHitPoints,
                "Temporary hit points must not be negative.");
        }

        if (deathSavingThrows.SuccessCount is < 0
            or >= DeathSavingThrowRules.SuccessesRequired)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                deathSavingThrows.SuccessCount,
                $"Death saving throw successes must be between 0 and {DeathSavingThrowRules.SuccessesRequired - 1}.");
        }

        if (deathSavingThrows.FailureCount is < 0
            or > DeathSavingThrowRules.FailuresRequired)
        {
            throw new ArgumentOutOfRangeException(
                nameof(health),
                deathSavingThrows.FailureCount,
                $"Death saving throw failures must be between 0 and {DeathSavingThrowRules.FailuresRequired}.");
        }

        if (deathSavingThrows.IsStable
            && deathSavingThrows.IsDead)
        {
            throw new ArgumentException(
                "A creature cannot be both stable and dead.",
                nameof(health));
        }

        if (deathSavingThrows.IsStable
            && (deathSavingThrows.SuccessCount != 0
                || deathSavingThrows.FailureCount != 0))
        {
            throw new ArgumentException(
                "A stable creature must have no recorded death saving throw successes or failures.",
                nameof(health));
        }

        if (!hitPoints.IsAtZeroHitPoints)
        {
            if (health.IsInstantlyDead)
            {
                throw new ArgumentException(
                    "An instantly dead creature must have 0 hit points.",
                    nameof(health));
            }

            if (deathSavingThrows.SuccessCount != 0
                || deathSavingThrows.FailureCount != 0
                || deathSavingThrows.IsStable)
            {
                throw new ArgumentException(
                    "A creature with hit points must have no death saving throw progress.",
                    nameof(health));
            }
        }

        if (health.IsInstantlyDead
            && deathSavingThrows.IsStable)
        {
            throw new ArgumentException(
                "An instantly dead creature cannot be stable.",
                nameof(health));
        }

        if (health.IsInstantlyDead
            && deathSavingThrows.IsDead)
        {
            throw new ArgumentException(
                "A creature cannot have both instant-death and failed-death-save terminal states.",
                nameof(health));
        }

        if (zeroHitPointPolicy
                == CombatantZeroHitPointPolicy.Defeated
            && (health.IsInstantlyDead
                || deathSavingThrows.SuccessCount != 0
                || deathSavingThrows.FailureCount != 0
                || deathSavingThrows.IsStable))
        {
            throw new ArgumentException(
                "A combatant defeated at 0 hit points cannot be dead or have death saving throw progress.",
                nameof(health));
        }
    }

    private static void ValidateAmmunition(
        AmmunitionState ammunition)
    {
        ArgumentNullException.ThrowIfNull(ammunition);

        if (string.IsNullOrWhiteSpace(
            ammunition.WeaponId))
        {
            throw new ArgumentException(
                "Ammunition weapon ID is required.",
                nameof(ammunition));
        }

        if (string.IsNullOrWhiteSpace(
            ammunition.AmmunitionItemId))
        {
            throw new ArgumentException(
                "Ammunition item ID is required.",
                nameof(ammunition));
        }

        if (ammunition.RemainingQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ammunition),
                ammunition.RemainingQuantity,
                "Remaining ammunition quantity must not be negative.");
        }
    }

    private static void ValidateScenario(
        WatchtowerScenarioState scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        if (!Enum.IsDefined(scenario.Progress))
        {
            throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario.Progress,
                "Unsupported watchtower scenario progress.");
        }
    }
}
