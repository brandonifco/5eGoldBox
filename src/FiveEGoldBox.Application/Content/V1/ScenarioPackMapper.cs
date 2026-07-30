using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Content.V1;

/// Turns one scenario content pack into the internal ScenarioDefinition the
/// engine actually loads. Unlike RulesetPackMapper there is no merge step --
/// a scenario is authored as one adventure, not assembled from packs -- so
/// this is a straight DTO-to-runtime translation.
internal static class ScenarioPackMapper
{
    internal const int FormatVersion = 1;

    internal static ScenarioDefinition ToRuntime(
        ScenarioPackV1 pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return new ScenarioDefinition
        {
            ScenarioId = pack.ScenarioId,
            DisplayName = pack.DisplayName,
            RulesetId = pack.RulesetId,
            StartingLocationId = pack.StartingLocationId,
            Progress = ToRuntimeProgress(pack.Progress),
            PartyRequirement = ToRuntimePartyRequirement(
                pack.PartyRequirement),
            Locations = pack.Locations
                .Select(ToRuntimeLocation)
                .ToArray(),
            Routes = pack.Routes
                .Select(ToRuntimeRoute)
                .ToArray(),
            Encounters = pack.Encounters
                .Select(ToRuntimeEncounter)
                .ToArray(),
            Triggers = pack.Triggers
                .Select(ToRuntimeTrigger)
                .ToArray(),
            Decisions = pack.Decisions
                .Select(ToRuntimeDecision)
                .ToArray()
        };
    }

    private static ScenarioProgressDefinition ToRuntimeProgress(
        ScenarioProgressDefinitionV1 progress)
    {
        return new ScenarioProgressDefinition
        {
            InitialProgressId = progress.InitialProgressId,
            ProgressIds = progress.ProgressIds.ToArray(),
            Conclusions = progress.Conclusions
                .Select(ToRuntimeConclusion)
                .ToArray()
        };
    }

    private static ScenarioConclusionDefinition ToRuntimeConclusion(
        ScenarioConclusionDefinitionV1 conclusion)
    {
        return new ScenarioConclusionDefinition
        {
            ProgressId = conclusion.ProgressId,
            IsSuccess = conclusion.IsSuccess,
            LocationId = conclusion.LocationId
        };
    }

    private static PartyRequirementDefinition ToRuntimePartyRequirement(
        PartyRequirementDefinitionV1 partyRequirement)
    {
        return new PartyRequirementDefinition
        {
            MinimumMembers = partyRequirement.MinimumMembers,
            MaximumMembers = partyRequirement.MaximumMembers,
            MinimumConsciousMembers =
                partyRequirement.MinimumConsciousMembers
        };
    }

    private static ScenarioLocationDefinition ToRuntimeLocation(
        ScenarioLocationDefinitionV1 location)
    {
        return new ScenarioLocationDefinition
        {
            LocationId = location.LocationId,
            DisplayName = location.DisplayName,
            ExplorationMap = location.ExplorationMap is null
                ? null
                : ToRuntimeExplorationMap(location.ExplorationMap),
            ExplorableProgressIds =
                location.ExplorableProgressIds.ToArray()
        };
    }

    private static ExplorationMapDefinition ToRuntimeExplorationMap(
        ExplorationMapDefinitionV1 map)
    {
        return new ExplorationMapDefinition
        {
            MapId = map.MapId,
            Width = map.Width,
            Height = map.Height,
            StartingFloor = map.StartingFloor,
            StartingPosition = ToRuntimePosition(map.StartingPosition),
            StartingFacing = ToRuntimeFacing(map.StartingFacing),
            Floors = map.Floors
                .Select(ToRuntimeExplorationFloor)
                .ToArray()
        };
    }

    private static ExplorationFloorDefinition ToRuntimeExplorationFloor(
        ExplorationFloorDefinitionV1 floor)
    {
        return new ExplorationFloorDefinition
        {
            Floor = floor.Floor,
            TraversablePositions = floor.TraversablePositions
                .Select(ToRuntimePosition)
                .ToArray(),
            Stairs = floor.Stairs
                .Select(ToRuntimeStair)
                .ToArray()
        };
    }

    private static StairDefinition ToRuntimeStair(
        StairDefinitionV1 stair)
    {
        return new StairDefinition
        {
            Position = ToRuntimePosition(stair.Position),
            DestinationFloor = stair.DestinationFloor,
            DestinationPosition = ToRuntimePosition(
                stair.DestinationPosition)
        };
    }

    private static TravelRouteDefinition ToRuntimeRoute(
        TravelRouteDefinitionV1 route)
    {
        return new TravelRouteDefinition
        {
            RouteId = route.RouteId,
            OriginLocationId = route.OriginLocationId,
            DestinationLocationId = route.DestinationLocationId,
            FinalStepIndex = route.FinalStepIndex,
            RequiredProgressIds = route.RequiredProgressIds.ToArray()
        };
    }

    private static EncounterDefinition ToRuntimeEncounter(
        EncounterDefinitionV1 encounter)
    {
        return new EncounterDefinition
        {
            EncounterId = encounter.EncounterId,
            BattlefieldId = encounter.BattlefieldId,
            Width = encounter.Width,
            Height = encounter.Height,
            PartySideId = encounter.PartySideId,
            BlockedPositions = encounter.BlockedPositions
                .Select(ToRuntimePosition)
                .ToArray(),
            PartyStartingPositions = encounter.PartyStartingPositions
                .Select(ToRuntimePosition)
                .ToArray(),
            Combatants = encounter.Combatants
                .Select(ToRuntimeEncounterCombatant)
                .ToArray(),
            Outcome = ToRuntimeOutcome(encounter.Outcome)
        };
    }

    private static EncounterCombatantDefinition ToRuntimeEncounterCombatant(
        EncounterCombatantDefinitionV1 combatant)
    {
        return new EncounterCombatantDefinition
        {
            CombatantId = combatant.CombatantId,
            MonsterId = combatant.MonsterId,
            SideId = combatant.SideId,
            StartingPosition = ToRuntimePosition(combatant.StartingPosition)
        };
    }

    private static EncounterOutcomeDefinition ToRuntimeOutcome(
        EncounterOutcomeDefinitionV1 outcome)
    {
        return new EncounterOutcomeDefinition
        {
            VictoryProgressId = outcome.VictoryProgressId,
            DefeatProgressId = outcome.DefeatProgressId
        };
    }

    private static ScenarioTriggerDefinition ToRuntimeTrigger(
        ScenarioTriggerDefinitionV1 trigger)
    {
        return new ScenarioTriggerDefinition
        {
            TriggerId = trigger.TriggerId,
            DisplayName = trigger.DisplayName,
            LocationId = trigger.LocationId,
            Floor = trigger.Floor,
            Position = trigger.Position is null
                ? null
                : ToRuntimePosition(trigger.Position),
            RequiredFacing = trigger.RequiredFacing is null
                ? null
                : ToRuntimeFacing(trigger.RequiredFacing.Value),
            RequiredProgressIds = trigger.RequiredProgressIds.ToArray(),
            ResultingProgressId = trigger.ResultingProgressId,
            EncounterId = trigger.EncounterId
        };
    }

    private static ScenarioDecisionDefinition ToRuntimeDecision(
        ScenarioDecisionDefinitionV1 decision)
    {
        return new ScenarioDecisionDefinition
        {
            DecisionId = decision.DecisionId,
            DisplayName = decision.DisplayName,
            LocationId = decision.LocationId,
            RequiredProgressIds = decision.RequiredProgressIds.ToArray(),
            Options = decision.Options
                .Select(ToRuntimeDecisionOption)
                .ToArray()
        };
    }

    private static ScenarioDecisionOptionDefinition ToRuntimeDecisionOption(
        ScenarioDecisionOptionDefinitionV1 option)
    {
        return new ScenarioDecisionOptionDefinition
        {
            OptionId = option.OptionId,
            DisplayName = option.DisplayName,
            ResultingProgressId = option.ResultingProgressId
        };
    }

    private static GridPosition ToRuntimePosition(
        GridPositionV1 position)
    {
        return new GridPosition(position.X, position.Y);
    }

    private static ExplorationFacing ToRuntimeFacing(
        ExplorationFacingV1 facing)
    {
        return facing switch
        {
            ExplorationFacingV1.North => ExplorationFacing.North,
            ExplorationFacingV1.East => ExplorationFacing.East,
            ExplorationFacingV1.South => ExplorationFacing.South,
            ExplorationFacingV1.West => ExplorationFacing.West,
            _ => throw new ArgumentOutOfRangeException(
                nameof(facing),
                facing,
                "Unsupported packed exploration facing.")
        };
    }

    internal static CombatantZeroHitPointPolicy
        ToRuntimeZeroHitPointPolicy(
            CombatantZeroHitPointPolicyV1 policy)
    {
        return policy switch
        {
            CombatantZeroHitPointPolicyV1.DeathSavingThrows =>
                CombatantZeroHitPointPolicy.DeathSavingThrows,
            CombatantZeroHitPointPolicyV1.Defeated =>
                CombatantZeroHitPointPolicy.Defeated,
            _ => throw new ArgumentOutOfRangeException(
                nameof(policy),
                policy,
                "Unsupported packed zero-hit-point policy.")
        };
    }
}
