using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Runtime;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Tests;

/// Content validation for authored scenarios. The baseline is the Watchtower
/// definition built in ScenarioDefinitionModelTests: it must pass cleanly, and
/// each rule is then proven by breaking exactly one thing about it.
public sealed class ScenarioDefinitionValidatorTests
{
    [Fact]
    public void Validate_AcceptsTheWatchtowerDefinition()
    {
        ValidationResult result = ScenarioDefinitionValidator.Validate(
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition());

        Assert.True(
            result.IsValid,
            "Watchtower should validate cleanly, but reported: "
                + string.Join(
                    "; ",
                    result.Issues.Select(issue => issue.Code)));
        Assert.Empty(result.Issues);
    }

    [Theory]
    [InlineData("scenario.id.required")]
    [InlineData("scenario.starting_location.unknown")]
    [InlineData("scenario.progress.initial_unknown")]
    [InlineData("scenario.conclusion.progress_unknown")]
    [InlineData("scenario.conclusion.initial_progress")]
    [InlineData("scenario.locations.duplicate_id")]
    [InlineData("scenario.map.starting_position_impassable")]
    [InlineData("scenario.map.position_out_of_bounds")]
    [InlineData("scenario.map.stair_destination_impassable")]
    [InlineData("scenario.routes.circular")]
    [InlineData("scenario.triggers.resulting_progress_unknown")]
    [InlineData("scenario.triggers.encounter_unknown")]
    [InlineData("scenario.triggers.position_impassable")]
    [InlineData("scenario.encounters.unreachable")]
    [InlineData("scenario.encounters.insufficient_deployment")]
    [InlineData("scenario.combatants.overlap")]
    [InlineData("scenario.combatants.party_side")]
    [InlineData("scenario.combatants.hit_points")]
    [InlineData("scenario.combatants.ammunition_incomplete")]
    [InlineData("scenario.party_requirement.minimum_members")]
    public void Validate_RejectsBrokenContent(string expectedCode)
    {
        ValidationResult result = ScenarioDefinitionValidator.Validate(
            BreakWatchtower(expectedCode));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == expectedCode);
    }

    /// Every issue is collected, so an author sees the whole list rather than
    /// fixing one mistake at a time.
    [Fact]
    public void Validate_ReportsEveryProblemAtOnce()
    {
        ScenarioDefinition definition = Mutate(
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition(),
            source => source with
            {
                StartingLocationId = "location.nowhere",
                Routes =
                [
                    source.Routes[0] with
                    {
                        OriginLocationId = "location.nowhere",
                        FinalStepIndex = 0
                    }
                ]
            });

        ValidationResult result =
            ScenarioDefinitionValidator.Validate(definition);

        Assert.True(result.Issues.Count >= 3);
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "scenario.starting_location.unknown");
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "scenario.routes.origin_unknown");
        Assert.Contains(
            result.Issues,
            issue => issue.Code == "scenario.routes.final_step");
    }

    /// Takes the known-good Watchtower definition and breaks exactly the one
    /// thing the given rule is supposed to catch.
    private static ScenarioDefinition BreakWatchtower(string code)
    {
        Dictionary<string, Func<ScenarioDefinition, ScenarioDefinition>>
            breakages = [];

        Add(breakages, "scenario.id.required",
            source => source with { ScenarioId = "  " });

        Add(breakages, "scenario.starting_location.unknown",
            source => source with { StartingLocationId = "location.nowhere" });

        Add(breakages, "scenario.progress.initial_unknown",
            source => source with
            {
                Progress = source.Progress with
                {
                    InitialProgressId = "NoSuchProgress"
                }
            });

        Add(breakages, "scenario.conclusion.progress_unknown",
            source => source with
            {
                Progress = source.Progress with
                {
                    Conclusions =
                    [
                        source.Progress.Conclusions[0] with
                        {
                            ProgressId = "NoSuchProgress"
                        }
                    ]
                }
            });

        // A scenario that begins already concluded can never be played.
        Add(breakages, "scenario.conclusion.initial_progress",
            source => source with
            {
                Progress = source.Progress with
                {
                    Conclusions =
                    [
                        source.Progress.Conclusions[0] with
                        {
                            ProgressId = source.Progress.InitialProgressId
                        }
                    ]
                }
            });

        Add(breakages, "scenario.locations.duplicate_id",
            source => source with
            {
                Locations = [source.Locations[0], source.Locations[0]]
            });

        Add(breakages, "scenario.map.starting_position_impassable",
            source => WithMap(
                source,
                map => map with
                {
                    StartingPosition = new GridPosition(4, 4)
                }));

        Add(breakages, "scenario.map.position_out_of_bounds",
            source => WithMap(
                source,
                map => map with { Width = 1, Height = 1 }));

        Add(breakages, "scenario.map.stair_destination_impassable",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Stairs =
                            [
                                map.Floors[0].Stairs[0] with
                                {
                                    DestinationPosition =
                                        new GridPosition(4, 4)
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.routes.circular",
            source => source with
            {
                Routes =
                [
                    source.Routes[0] with
                    {
                        DestinationLocationId =
                            source.Routes[0].OriginLocationId
                    }
                ]
            });

        Add(breakages, "scenario.triggers.resulting_progress_unknown",
            source => source with
            {
                Triggers =
                [
                    source.Triggers[0] with
                    {
                        ResultingProgressId = "NoSuchProgress"
                    }
                ]
            });

        Add(breakages, "scenario.triggers.encounter_unknown",
            source => source with
            {
                Triggers =
                [
                    source.Triggers[0] with
                    {
                        EncounterId = "encounter.nowhere"
                    }
                ]
            });

        // A trigger sitting where the party can never stand is unreachable.
        Add(breakages, "scenario.triggers.position_impassable",
            source => source with
            {
                Triggers =
                [
                    source.Triggers[0] with
                    {
                        Position = new GridPosition(4, 4)
                    }
                ]
            });

        Add(breakages, "scenario.encounters.unreachable",
            source => source with { Triggers = [] });

        Add(breakages, "scenario.encounters.insufficient_deployment",
            source => source with
            {
                PartyRequirement = source.PartyRequirement with
                {
                    MaximumMembers = 6
                }
            });

        Add(breakages, "scenario.combatants.overlap",
            source => WithCombatants(
                source,
                combatants =>
                [
                    combatants[0],
                    combatants[1] with
                    {
                        StartingPosition = combatants[0].StartingPosition
                    }
                ]));

        Add(breakages, "scenario.combatants.party_side",
            source => WithCombatants(
                source,
                combatants =>
                [
                    combatants[0] with
                    {
                        SideId = source.Encounters[0].PartySideId
                    },
                    combatants[1]
                ]));

        Add(breakages, "scenario.combatants.hit_points",
            source => WithCombatants(
                source,
                combatants =>
                [
                    combatants[0] with { MaximumHitPoints = 0 },
                    combatants[1]
                ]));

        // Half-declared ammunition is an authoring slip, not a valid weapon.
        Add(breakages, "scenario.combatants.ammunition_incomplete",
            source => WithCombatants(
                source,
                combatants =>
                [
                    combatants[0],
                    combatants[1] with
                    {
                        Weapons =
                        [
                            combatants[1].Weapons[0] with
                            {
                                AmmunitionQuantity = null
                            }
                        ]
                    }
                ]));

        Add(breakages, "scenario.party_requirement.minimum_members",
            source => source with
            {
                PartyRequirement = source.PartyRequirement with
                {
                    MinimumMembers = 0
                }
            });

        return breakages[code](
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition());
    }

    private static void Add(
        Dictionary<string, Func<ScenarioDefinition, ScenarioDefinition>> breakages,
        string expectedCode,
        Func<ScenarioDefinition, ScenarioDefinition> breakage)
    {
        breakages.Add(expectedCode, breakage);
    }

    private static ScenarioDefinition Mutate(
        ScenarioDefinition source,
        Func<ScenarioDefinition, ScenarioDefinition> change)
    {
        return change(source);
    }

    private static ScenarioDefinition WithMap(
        ScenarioDefinition source,
        Func<ExplorationMapDefinition, ExplorationMapDefinition> change)
    {
        ScenarioLocationDefinition[] locations = source.Locations
            .Select(location => location.ExplorationMap is null
                ? location
                : location with
                {
                    ExplorationMap = change(location.ExplorationMap)
                })
            .ToArray();

        return source with { Locations = locations };
    }

    private static ScenarioDefinition WithCombatants(
        ScenarioDefinition source,
        Func<IReadOnlyList<CombatantDefinition>,
            IReadOnlyList<CombatantDefinition>> change)
    {
        return source with
        {
            Encounters =
            [
                source.Encounters[0] with
                {
                    Combatants = change(source.Encounters[0].Combatants)
                }
            ]
        };
    }
}
