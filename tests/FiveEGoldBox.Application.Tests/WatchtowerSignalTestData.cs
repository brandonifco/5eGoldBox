using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

internal static class WatchtowerSignalTestData
{
    internal static ApplicationSessionState
        CreateEncounterSession()
    {
        return SignalMechanismRules.Activate(
            CreateSignalReadySession(),
            CreateRuleset());
    }

    internal static ApplicationSessionState
        CreateSignalReadySession()
    {
        ApplicationSessionState current =
            MoveToUpperFloorStair();

        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Right);
        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);
        current = ExplorationRules.Turn(
            current,
            ExplorationTurnDirection.Left);

        return current;
    }

    internal static ApplicationSessionState
        CreateSignalReadySessionWithStableAndDyingParty()
    {
        ApplicationSessionState state =
            CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();

        members[0] = members[0] with
        {
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 12,
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 0
                },
                DeathSavingThrows =
                    new DeathSavingThrowState
                    {
                        SuccessCount = 0,
                        FailureCount = 0,
                        IsStable = true
                    },
                IsInstantlyDead = false
            }
        };
        members[1] = members[1] with
        {
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 14,
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 0
                },
                DeathSavingThrows =
                    new DeathSavingThrowState
                    {
                        SuccessCount = 1,
                        FailureCount = 2,
                        IsStable = false
                    },
                IsInstantlyDead = false
            }
        };

        return state with
        {
            Party = state.Party with
            {
                Members = Array.AsReadOnly(members)
            }
        };
    }

    internal static ApplicationSessionState
        MoveToUpperFloorStair()
    {
        ApplicationSessionState current =
            CreateExplorationSession();

        current = ExplorationRules.MoveForward(current)
            .State;
        current = ExplorationRules.MoveForward(current)
            .State;

        return ExplorationRules.UseStairs(current);
    }

    internal static ApplicationSessionState
        CreateExplorationSession()
    {
        return ExplorationRules.EnterWatchtower(
            CreateCompletedArrival());
    }

    internal static ApplicationSessionState
        CreateAcceptedSession()
    {
        return OutpostMissionRules.Resolve(
            CreateMissionNotAcceptedSession() with
            {
                RandomValuesConsumed = 12
            },
            OutpostMissionChoice.AcceptMission)
                .State;
    }

    internal static ApplicationSessionState
        CreateRegionalTravelSession()
    {
        return RegionalTravelRules.BeginWatchtowerJourney(
            CreateAcceptedSession());
    }

    internal static ValidatedRuleset CreateRuleset(
        bool includeLongbow = true)
    {
        List<WeaponDefinition> weapons =
        [
            CreateMeleeWeapon(
                "weapon.longsword",
                "Longsword",
                DieType.D8),
            CreateMeleeWeapon(
                "weapon.greataxe",
                "Greataxe",
                DieType.D12)
        ];

        if (includeLongbow)
        {
            weapons.Add(new WeaponDefinition
            {
                Id = "weapon.longbow",
                Name = "Longbow",
                Category = WeaponCategory.Martial,
                AttackKind = WeaponAttackKind.Ranged,
                Damage = new DamageDice
                {
                    Count = 1,
                    Die = DieType.D8
                },
                DamageType = "damage.piercing",
                Properties =
                [
                    RuleIds.WeaponProperties.Ammunition,
                    RuleIds.WeaponProperties.Heavy,
                    RuleIds.WeaponProperties.TwoHanded
                ],
                NormalRangeFeet = 150,
                LongRangeFeet = 600,
                AmmunitionItemId = "item.arrow"
            });
        }

        RulesetDefinition definition = new()
        {
            Id = "ruleset.watchtower-test",
            Name = "Watchtower Test Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = "race.human",
                    Name = "Human",
                    BaseSpeedFeet = 30,
                    AbilityScoreIncreases =
                    [
                        new AbilityScoreIncrease(
                            Ability.Strength,
                            1),
                        new AbilityScoreIncrease(
                            Ability.Dexterity,
                            1),
                        new AbilityScoreIncrease(
                            Ability.Constitution,
                            1),
                        new AbilityScoreIncrease(
                            Ability.Intelligence,
                            1),
                        new AbilityScoreIncrease(
                            Ability.Wisdom,
                            1),
                        new AbilityScoreIncrease(
                            Ability.Charisma,
                            1)
                    ]
                }
            ],
            Classes = CreateClasses(),
            Backgrounds =
            [
                new BackgroundDefinition
                {
                    Id = "background.soldier",
                    Name = "Soldier"
                }
            ],
            Skills = CreateSkills(),
            Weapons = weapons,
            EquipmentItems =
            [
                new EquipmentItemDefinition
                {
                    Id = "item.arrow",
                    Name = "Arrow",
                    WeightPounds = 0.05m
                }
            ]
        };

        RulesetLoadResult load =
            ValidatedRuleset.Load(definition);

        Assert.True(load.IsValid);

        return Assert.IsType<ValidatedRuleset>(
            load.Ruleset);
    }

    internal static CharacterDraft CreateExpectedDraft(
        PartyMemberState member)
    {
        return member.CharacterDefinitionId switch
        {
            "character.fighter" => CreateExpectedDraft(
                member,
                "class.fighter",
                "weapon.longsword",
                new Dictionary<Ability, int>
                {
                    [Ability.Strength] = 15,
                    [Ability.Dexterity] = 11,
                    [Ability.Constitution] = 13,
                    [Ability.Intelligence] = 7,
                    [Ability.Wisdom] = 9,
                    [Ability.Charisma] = 12
                },
                ["skill.athletics", "skill.perception"],
                Array.Empty<InventoryItemDraft>()),
            "character.barbarian" => CreateExpectedDraft(
                member,
                "class.barbarian",
                "weapon.greataxe",
                new Dictionary<Ability, int>
                {
                    [Ability.Strength] = 15,
                    [Ability.Dexterity] = 13,
                    [Ability.Constitution] = 13,
                    [Ability.Intelligence] = 7,
                    [Ability.Wisdom] = 11,
                    [Ability.Charisma] = 9
                },
                ["skill.athletics", "skill.survival"],
                Array.Empty<InventoryItemDraft>()),
            "character.ranger" => CreateExpectedDraft(
                member,
                "class.ranger",
                "weapon.longbow",
                new Dictionary<Ability, int>
                {
                    [Ability.Strength] = 11,
                    [Ability.Dexterity] = 15,
                    [Ability.Constitution] = 11,
                    [Ability.Intelligence] = 9,
                    [Ability.Wisdom] = 15,
                    [Ability.Charisma] = 7
                },
                [
                    "skill.perception",
                    "skill.stealth",
                    "skill.survival"
                ],
                member.Ammunition!.RemainingQuantity == 0
                    ? Array.Empty<InventoryItemDraft>()
                    :
                    [
                        new InventoryItemDraft
                        {
                            ItemId = "item.arrow",
                            Quantity = member.Ammunition
                                .RemainingQuantity
                        }
                    ]),
            _ => throw new InvalidOperationException(
                "Unsupported test character definition.")
        };
    }

    private static ApplicationSessionState
        CreateCompletedArrival()
    {
        ApplicationSessionState current =
            RegionalTravelRules.BeginWatchtowerJourney(
                CreateAcceptedSession());

        while (!Assert.IsType<RegionalTravelState>(
            current.RegionalTravel).IsComplete)
        {
            current = RegionalTravelRules.Advance(current)
                .State;
        }

        return current;
    }

    private static ApplicationSessionState
        CreateMissionNotAcceptedSession()
    {
        PartyMemberState[] members =
        [
            CreateMember(
                "party-member.fighter",
                "character.fighter",
                "Fighter",
                "class.fighter",
                maximumHitPoints: 12) with
            {
                Health = CombatantHealthRules.Create(12) with
                {
                    HitPoints = new HitPointState
                    {
                        MaximumHitPoints = 12,
                        CurrentHitPoints = 8,
                        TemporaryHitPoints = 2
                    }
                }
            },
            CreateMember(
                "party-member.barbarian",
                "character.barbarian",
                "Barbarian",
                "class.barbarian",
                maximumHitPoints: 14),
            CreateMember(
                "party-member.ranger",
                "character.ranger",
                "Ranger",
                "class.ranger",
                maximumHitPoints: 11) with
            {
                Ammunition = new AmmunitionState
                {
                    WeaponId = "weapon.longbow",
                    AmmunitionItemId = "item.arrow",
                    RemainingQuantity = 7
                }
            }
        ];

        return ApplicationSessionRules.CreateNew(
            scenarioId: "scenario.watchtower",
            currentLocationId: "location.outpost",
            party: new PartyState
            {
                PartyId = "party.player",
                Members = members
            },
            randomSeed: 8675309);
    }

    private static PartyMemberState CreateMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId =
                characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = CombatantHealthRules.Create(
                maximumHitPoints),
            Ammunition = null
        };
    }

    private static CharacterDraft CreateExpectedDraft(
        PartyMemberState member,
        string classId,
        string weaponId,
        IReadOnlyDictionary<Ability, int> abilityScores,
        IReadOnlyList<string> selectedSkills,
        IReadOnlyList<InventoryItemDraft> inventoryItems)
    {
        return new CharacterDraft
        {
            Name = member.DisplayName,
            Level = 1,
            RaceId = "race.human",
            ClassId = classId,
            BackgroundId = "background.soldier",
            AbilityScoreGenerationMethod =
                AbilityScoreGenerationMethod.Manual,
            BaseAbilityScores = abilityScores,
            SelectedSkillIds = selectedSkills,
            EquippedWeaponIds =
            [
                weaponId
            ],
            InventoryItems = inventoryItems
        };
    }

    private static IReadOnlyList<ClassDefinition>
        CreateClasses()
    {
        return
        [
            CreateClass(
                "class.fighter",
                "Fighter",
                DieType.D10,
                [Ability.Strength, Ability.Constitution],
                ["skill.athletics", "skill.perception"]),
            CreateClass(
                "class.barbarian",
                "Barbarian",
                DieType.D12,
                [Ability.Strength, Ability.Constitution],
                ["skill.athletics", "skill.survival"]),
            CreateClass(
                "class.ranger",
                "Ranger",
                DieType.D10,
                [Ability.Strength, Ability.Dexterity],
                [
                    "skill.perception",
                    "skill.stealth",
                    "skill.survival"
                ])
        ];
    }

    private static ClassDefinition CreateClass(
        string id,
        string name,
        DieType hitDie,
        IReadOnlyList<Ability> savingThrows,
        IReadOnlyList<string> skills)
    {
        return new ClassDefinition
        {
            Id = id,
            Name = name,
            HitDie = hitDie,
            SavingThrowProficiencies = savingThrows,
            WeaponProficiencies =
            [
                RuleIds.WeaponProficiencies.Simple,
                RuleIds.WeaponProficiencies.Martial
            ],
            SkillChoices = skills,
            NumberOfSkillChoices = skills.Count
        };
    }

    private static IReadOnlyList<SkillDefinition>
        CreateSkills()
    {
        return
        [
            new SkillDefinition
            {
                Id = "skill.athletics",
                Name = "Athletics",
                Ability = Ability.Strength
            },
            new SkillDefinition
            {
                Id = "skill.perception",
                Name = "Perception",
                Ability = Ability.Wisdom
            },
            new SkillDefinition
            {
                Id = "skill.stealth",
                Name = "Stealth",
                Ability = Ability.Dexterity
            },
            new SkillDefinition
            {
                Id = "skill.survival",
                Name = "Survival",
                Ability = Ability.Wisdom
            }
        ];
    }

    private static WeaponDefinition CreateMeleeWeapon(
        string id,
        string name,
        DieType damageDie)
    {
        return new WeaponDefinition
        {
            Id = id,
            Name = name,
            Category = WeaponCategory.Martial,
            AttackKind = WeaponAttackKind.Melee,
            Damage = new DamageDice
            {
                Count = 1,
                Die = damageDie
            },
            DamageType = "damage.slashing",
            ReachFeet = 5
        };
    }
}
