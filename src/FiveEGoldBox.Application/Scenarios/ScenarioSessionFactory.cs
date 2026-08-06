using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

/// Starts a new session for whichever scenario is asked for.
///
/// A client names a scenario and gets a session; it does not need to know
/// which scenario that is or what content backs it.
public static class ScenarioSessionFactory
{
    public static ApplicationSessionState CreateNew(
        string scenarioId,
        int randomSeed)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(scenarioId);

        // The party comes from the campaign the scenario belongs to. Which
        // characters attempt an adventure is a campaign's business, not the
        // adventure's.
        CampaignDefinition campaign =
            CampaignRegistry.ResolveForScenario(scenarioId);

        return ApplicationSessionRules.CreateNew(
            scenario.ScenarioId,
            scenario.StartingLocationId,
            CampaignPartyFactory.CreateStartingParty(campaign),
            randomSeed);
    }

    /// The same as CreateNew above, except the party comes from
    /// CharacterCreationRules.CreateParty (or anywhere else that produces a
    /// real PartyState) rather than the campaign's own authored roster.
    ///
    /// The party still has to be one the campaign could field --
    /// CampaignPartyCompositionValidator checks its size against
    /// CampaignDefinition.ActivePartySize the same way it would for the
    /// roster path, it just resolves each member's build (class, max hit
    /// points, resources, ammunition) against PartyMemberState.CustomBuild
    /// instead of a roster entry.
    public static ApplicationSessionState CreateNew(
        string scenarioId,
        int randomSeed,
        PartyState party)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        ArgumentNullException.ThrowIfNull(party);

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(scenarioId);

        return ApplicationSessionRules.CreateNew(
            scenario.ScenarioId,
            scenario.StartingLocationId,
            party,
            randomSeed);
    }

    /// Starts a session already inside a specific exploration location, at a
    /// specific floor/position/facing, bypassing the outpost decision and
    /// regional travel a normal session would have to walk through first.
    ///
    /// A developer tool, not a player-facing entry point: it exists so a spot
    /// on a map can be reached directly (e.g. to check rendering of a door or
    /// piece of treasure) without replaying the whole scenario up to it every
    /// time. The floor/position still have to be real, traversable geometry —
    /// <see cref="ApplicationSessionRules.CreateCanonical"/> is what actually
    /// enforces that, the same as it does for <see cref="CreateNew"/>.
    public static ApplicationSessionState CreateAtExploration(
        string scenarioId,
        string locationId,
        string floor,
        GridPosition position,
        ExplorationFacing facing,
        int randomSeed,
        string? progressId = null)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        if (string.IsNullOrWhiteSpace(locationId))
        {
            throw new ArgumentException(
                "Location ID is required.",
                nameof(locationId));
        }

        if (string.IsNullOrWhiteSpace(floor))
        {
            throw new ArgumentException(
                "Floor is required.",
                nameof(floor));
        }

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(scenarioId);

        // The party comes from the campaign the scenario belongs to, same as
        // CreateNew above.
        CampaignDefinition campaign =
            CampaignRegistry.ResolveForScenario(scenarioId);

        ScenarioLocationDefinition location = scenario.Locations
            .FirstOrDefault(candidate => string.Equals(
                candidate.LocationId,
                locationId,
                StringComparison.Ordinal))
            ?? throw new ArgumentException(
                $"Scenario '{scenarioId}' has no location '{locationId}'.",
                nameof(locationId));

        ExplorationMapDefinition explorationMap =
            location.ExplorationMap
            ?? throw new ArgumentException(
                $"Location '{locationId}' has no explorable map.",
                nameof(locationId));

        ApplicationSessionState state = new()
        {
            ScenarioId = scenario.ScenarioId,
            CurrentMode = ApplicationMode.Exploration,
            CurrentLocationId = locationId,
            Party = CampaignPartyFactory.CreateStartingParty(campaign),
            Scenario = new ScenarioState
            {
                ProgressId = progressId
                    ?? scenario.Progress.InitialProgressId
            },
            RandomSeed = randomSeed,
            RandomValuesConsumed = 0,
            Exploration = new ExplorationState
            {
                MapId = explorationMap.MapId,
                Floor = floor,
                Position = position,
                Facing = facing
            }
        };

        return ApplicationSessionRules.CreateCanonical(state);
    }
}
