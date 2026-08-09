using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
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
            "Watchtower should carry no errors, but reported: "
                + string.Join(
                    "; ",
                    result.Issues.Select(issue => issue.Code)));

        // Warnings are expected here: the scenario declares the full saved
        // progress vocabulary, two markers of which nothing yet produces.
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Severity == ValidationSeverity.Error);
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
    [InlineData("scenario.map.door_id_required")]
    [InlineData("scenario.map.duplicate_door_id")]
    [InlineData("scenario.map.door_position_out_of_bounds")]
    [InlineData("scenario.map.door_side_invalid")]
    [InlineData("scenario.map.door_edge_untraversable")]
    [InlineData("scenario.map.door_edge_collision")]
    [InlineData("scenario.map.treasure_id_required")]
    [InlineData("scenario.map.duplicate_treasure_id")]
    [InlineData("scenario.map.treasure_position_out_of_bounds")]
    [InlineData("scenario.map.treasure_position_unreachable")]
    [InlineData("scenario.map.treasure_position_collision")]
    [InlineData("scenario.map.treasure_no_reward")]
    [InlineData("scenario.map.treasure_gold_negative")]
    [InlineData("scenario.map.treasure_quantity_without_item")]
    [InlineData("scenario.map.treasure_quantity_not_positive")]
    [InlineData("scenario.routes.circular")]
    [InlineData("scenario.routes.origin_unreachable")]
    [InlineData("scenario.triggers.resulting_progress_unknown")]
    [InlineData("scenario.triggers.encounter_unknown")]
    [InlineData("scenario.triggers.position_impassable")]
    [InlineData("scenario.encounters.unreachable")]
    [InlineData("scenario.encounters.insufficient_deployment")]
    [InlineData("scenario.combatants.overlap")]
    [InlineData("scenario.combatants.party_side")]
    [InlineData("scenario.combatants.monster_id_required")]
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

    /// A second route from the same origin as the first — a choice of two
    /// roads to the same place — is exactly what RegionalTravelRules can now
    /// run, and carries none of the reachability problem a return route has.
    [Fact]
    public void Validate_AcceptsASecondRouteFromTheSameOrigin()
    {
        ScenarioDefinition definition = Mutate(
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition(),
            source => source with
            {
                Routes =
                [
                    .. source.Routes,
                    source.Routes[0] with
                    {
                        RouteId = source.Routes[0].RouteId + ".shortcut",
                        FinalStepIndex =
                            source.Routes[0].FinalStepIndex - 1
                    }
                ]
            });

        ValidationResult result = ScenarioDefinitionValidator.Validate(
            definition);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Severity == ValidationSeverity.Error);
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

    /// New behaviour, and not reachable through the Theory list above -- that
    /// list validates a scenario alone, with no ruleset supplied, and the
    /// check below only runs when one is available.
    [Fact]
    public void Validate_ReportsAMonsterIdThatDoesNotResolveAgainstTheRuleset()
    {
        ScenarioDefinition definition =
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition();
        ValidatedRuleset ruleset = CreateRulesetWithMonsters([]);

        ValidationResult result = ScenarioDefinitionValidator.Validate(
            definition,
            ruleset);

        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "scenario.combatants.monster_id_unresolved");
    }

    /// The same definition against a ruleset that actually declares the
    /// monsters it references resolves cleanly -- proof the check above is a
    /// real cross-reference rather than one that always fires.
    [Fact]
    public void Validate_AcceptsAMonsterIdThatResolvesAgainstTheRuleset()
    {
        ScenarioDefinition definition =
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition();
        ValidatedRuleset ruleset = CreateRulesetWithMonsters(
            definition.Encounters[0].Combatants
                .Select(combatant => combatant.MonsterId)
                .ToArray());

        ValidationResult result = ScenarioDefinitionValidator.Validate(
            definition,
            ruleset);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code
                == "scenario.combatants.monster_id_unresolved");
    }

    /// New behaviour, and not reachable through the Theory list above -- same
    /// reasoning as the monster cross-reference check just above: it only
    /// fires when a ruleset is actually supplied.
    [Fact]
    public void Validate_ReportsATreasureItemIdThatDoesNotResolveAgainstTheRuleset()
    {
        ScenarioDefinition definition = WithTreasureItemId(
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition(),
            "item.unresolved");
        ValidatedRuleset ruleset = CreateRulesetWithEquipmentItems([]);

        ValidationResult result = ScenarioDefinitionValidator.Validate(
            definition,
            ruleset);

        Assert.Contains(
            result.Issues,
            issue => issue.Code
                == "scenario.map.treasure_item_id_unresolved");
    }

    /// The same definition against a ruleset that actually declares the item
    /// it references resolves cleanly -- proof the check above is a real
    /// cross-reference rather than one that always fires.
    [Fact]
    public void Validate_AcceptsATreasureItemIdThatResolvesAgainstTheRuleset()
    {
        const string itemId = "item.test.resolved";
        ScenarioDefinition definition = WithTreasureItemId(
            ScenarioDefinitionModelTests.CreateWatchtowerDefinition(),
            itemId);
        ValidatedRuleset ruleset = CreateRulesetWithEquipmentItems([itemId]);

        ValidationResult result = ScenarioDefinitionValidator.Validate(
            definition,
            ruleset);

        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code
                == "scenario.map.treasure_item_id_unresolved");
    }

    private static ScenarioDefinition WithTreasureItemId(
        ScenarioDefinition source,
        string itemId)
    {
        return WithMap(
            source,
            map => map with
            {
                Floors =
                [
                    map.Floors[0] with
                    {
                        Treasures =
                        [
                            new TreasureDefinition
                            {
                                TreasureId = "treasure.test.item_reference",
                                Position = new GridPosition(0, 0),
                                ItemId = itemId
                            }
                        ]
                    },
                    map.Floors[1]
                ]
            });
    }

    private static ValidatedRuleset CreateRulesetWithEquipmentItems(
        IReadOnlyList<string> itemIds)
    {
        RulesetLoadResult result = ValidatedRuleset.Load(new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            EquipmentItems = itemIds
                .Select(itemId => new EquipmentItemDefinition
                {
                    Id = itemId,
                    Name = itemId
                })
                .ToArray()
        });

        Assert.True(
            result.IsValid,
            "Test ruleset should carry no errors, but reported: "
                + string.Join(
                    "; ",
                    result.Validation.Issues.Select(issue => issue.Code)));

        return result.Ruleset!;
    }

    private static ValidatedRuleset CreateRulesetWithMonsters(
        IReadOnlyList<string> monsterIds)
    {
        const string weaponId = "weapon.test";

        RulesetLoadResult result = ValidatedRuleset.Load(new RulesetDefinition
        {
            Id = "ruleset.test",
            Name = "Test Ruleset",
            Weapons =
            [
                new WeaponDefinition
                {
                    Id = weaponId,
                    Name = "Test Weapon",
                    Category = WeaponCategory.Simple,
                    AttackKind = WeaponAttackKind.Melee,
                    Damage = new DamageDice { Count = 1, Die = DieType.D4 },
                    DamageType = "damage.bludgeoning"
                }
            ],
            Monsters = monsterIds
                .Select(monsterId => new MonsterDefinition
                {
                    Id = monsterId,
                    Name = monsterId,
                    MaximumHitPoints = 1,
                    ArmorClass = 10,
                    MovementSpeedFeet = 30,
                    ZeroHitPointPolicy =
                        CombatantZeroHitPointPolicy.Defeated,
                    AbilityModifiers = Enum.GetValues<Ability>()
                        .Select(ability => new MonsterAbilityModifier
                        {
                            Ability = ability,
                            Modifier = 0
                        })
                        .ToArray(),
                    ProficiencyBonus = 2,
                    ExperienceValue = 25,
                    Weapons =
                    [
                        new MonsterWeaponDefinition { WeaponId = weaponId }
                    ]
                })
                .ToArray()
        });

        Assert.True(
            result.IsValid,
            "Test ruleset should carry no errors, but reported: "
                + string.Join(
                    "; ",
                    result.Validation.Issues.Select(issue => issue.Code)));

        return result.Ruleset!;
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

        // (0, 0)-East-(1, 0) is the ground floor's only real edge -- both
        // ends are traversable, so this is a clean, otherwise-valid door
        // used to isolate just the blank-ID check.
        Add(breakages, "scenario.map.door_id_required",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "  ",
                                    Position = new GridPosition(0, 0),
                                    Side = ExplorationFacing.East,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.duplicate_door_id",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "door.test.duplicate",
                                    Position = new GridPosition(0, 0),
                                    Side = ExplorationFacing.East,
                                    IsSecret = false,
                                    IsLocked = false
                                },
                                new DoorDefinition
                                {
                                    DoorId = "door.test.duplicate",
                                    Position = new GridPosition(0, 0),
                                    Side = ExplorationFacing.East,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.door_position_out_of_bounds",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "door.test.out_of_bounds",
                                    Position = new GridPosition(99, 99),
                                    Side = ExplorationFacing.East,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.door_side_invalid",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "door.test.bad_side",
                                    Position = new GridPosition(0, 0),
                                    Side = (ExplorationFacing)99,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        // (0, 1) is in bounds but not traversable -- a door cannot connect
        // to a tile the party could never stand on regardless of it.
        Add(breakages, "scenario.map.door_edge_untraversable",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "door.test.untraversable",
                                    Position = new GridPosition(0, 0),
                                    Side = ExplorationFacing.South,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.door_edge_collision",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Doors =
                            [
                                new DoorDefinition
                                {
                                    DoorId = "door.test.collision_a",
                                    Position = new GridPosition(0, 0),
                                    Side = ExplorationFacing.East,
                                    IsSecret = false,
                                    IsLocked = false
                                },
                                new DoorDefinition
                                {
                                    DoorId = "door.test.collision_b",
                                    Position = new GridPosition(1, 0),
                                    Side = ExplorationFacing.West,
                                    IsSecret = false,
                                    IsLocked = false
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_id_required",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "  ",
                                    Position = new GridPosition(0, 0),
                                    GoldPieces = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.duplicate_treasure_id",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.duplicate",
                                    Position = new GridPosition(0, 0),
                                    GoldPieces = 1
                                },
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.duplicate",
                                    Position = new GridPosition(0, 0),
                                    GoldPieces = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_position_out_of_bounds",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.out_of_bounds",
                                    Position = new GridPosition(99, 99),
                                    GoldPieces = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        // (2, 2) is not declared traversable on the ground floor fixture.
        Add(breakages, "scenario.map.treasure_position_unreachable",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.unreachable",
                                    Position = new GridPosition(2, 2),
                                    GoldPieces = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        // (1, 0) is the ground floor's own stair square.
        Add(breakages, "scenario.map.treasure_position_collision",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.collision",
                                    Position = new GridPosition(1, 0),
                                    GoldPieces = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_no_reward",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.no_reward",
                                    Position = new GridPosition(0, 0)
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_gold_negative",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.negative_gold",
                                    Position = new GridPosition(0, 0),
                                    GoldPieces = -1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_quantity_without_item",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.quantity_without_item",
                                    Position = new GridPosition(0, 0),
                                    GoldPieces = 1,
                                    Quantity = 1
                                }
                            ]
                        },
                        map.Floors[1]
                    ]
                }));

        Add(breakages, "scenario.map.treasure_quantity_not_positive",
            source => WithMap(
                source,
                map => map with
                {
                    Floors =
                    [
                        map.Floors[0] with
                        {
                            Treasures =
                            [
                                new TreasureDefinition
                                {
                                    TreasureId = "treasure.test.quantity_not_positive",
                                    Position = new GridPosition(0, 0),
                                    ItemId = "item.test.quantity",
                                    Quantity = 0
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

        // A second, otherwise-well-formed route back the way the party came
        // is legal shape-wise (RegionalTravelRules can run more than one
        // route now), but nothing transitions the party back into outpost
        // mode at the destination to ever attempt it from there.
        Add(breakages, "scenario.routes.origin_unreachable",
            source => source with
            {
                Routes =
                [
                    .. source.Routes,
                    source.Routes[0] with
                    {
                        RouteId =
                            source.Routes[0].RouteId + ".return",
                        OriginLocationId =
                            source.Routes[0].DestinationLocationId,
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

        Add(breakages, "scenario.combatants.monster_id_required",
            source => WithCombatants(
                source,
                combatants =>
                [
                    combatants[0] with { MonsterId = "  " },
                    combatants[1]
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
        Func<IReadOnlyList<EncounterCombatantDefinition>,
            IReadOnlyList<EncounterCombatantDefinition>> change)
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
