using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios;

internal static class WatchtowerScenarioContent
{
    internal const string ScenarioId =
        "scenario.watchtower";

    internal const string OutpostLocationId =
        "location.outpost";

    private const string PartyId =
        "party.player";

    private const string FighterPartyMemberId =
        "party-member.fighter";

    private const string BarbarianPartyMemberId =
        "party-member.barbarian";

    private const string RangerPartyMemberId =
        "party-member.ranger";

    private const string HumanRaceId =
        "race.human";

    private const string SoldierBackgroundId =
        "background.soldier";

    private const string RulesetId =
        "ruleset.watchtower-test";

    internal static PartyState CreateStartingParty()
    {
        return new PartyState
        {
            PartyId = PartyId,
            Members =
            [
                CreatePartyMember(
                    FighterPartyMemberId,
                    WatchtowerPartyDefinitions
                        .FighterDefinitionId,
                    "Fighter",
                    WatchtowerPartyDefinitions
                        .FighterClassId,
                    maximumHitPoints: 12,
                    currentHitPoints: 8,
                    temporaryHitPoints: 2,
                    ammunition: null),
                CreatePartyMember(
                    BarbarianPartyMemberId,
                    WatchtowerPartyDefinitions
                        .BarbarianDefinitionId,
                    "Barbarian",
                    WatchtowerPartyDefinitions
                        .BarbarianClassId,
                    maximumHitPoints: 14,
                    currentHitPoints: 14,
                    temporaryHitPoints: 0,
                    ammunition: null),
                CreatePartyMember(
                    RangerPartyMemberId,
                    WatchtowerPartyDefinitions
                        .RangerDefinitionId,
                    "Ranger",
                    WatchtowerPartyDefinitions
                        .RangerClassId,
                    maximumHitPoints: 11,
                    currentHitPoints: 11,
                    temporaryHitPoints: 0,
                    ammunition: new AmmunitionState
                    {
                        WeaponId =
                            WatchtowerPartyDefinitions
                                .RangerWeaponId,
                        AmmunitionItemId =
                            WatchtowerPartyDefinitions
                                .RangerAmmunitionItemId,
                        RemainingQuantity = 7
                    })
            ]
        };
    }

    internal static ValidatedRuleset CreateRuleset()
    {
        RulesetDefinition definition = new()
        {
            Id = RulesetId,
            Name = "Watchtower Test Ruleset",
            Races =
            [
                new RaceDefinition
                {
                    Id = HumanRaceId,
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
                    Id = SoldierBackgroundId,
                    Name = "Soldier"
                }
            ],
            Skills = CreateSkills(),
            Weapons = CreateWeapons(),
            EquipmentItems =
            [
                new EquipmentItemDefinition
                {
                    Id = WatchtowerPartyDefinitions
                        .RangerAmmunitionItemId,
                    Name = "Arrow",
                    WeightPounds = 0.05m
                }
            ]
        };

        RulesetLoadResult load =
            ValidatedRuleset.Load(definition);

        if (!load.IsValid || load.Ruleset is null)
        {
            throw new InvalidOperationException(
                "The canonical watchtower ruleset could not be validated.");
        }

        return load.Ruleset;
    }

    private static PartyMemberState CreatePartyMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints,
        int currentHitPoints,
        int temporaryHitPoints,
        AmmunitionState? ammunition)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId = characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints =
                        maximumHitPoints,
                    CurrentHitPoints =
                        currentHitPoints,
                    TemporaryHitPoints =
                        temporaryHitPoints
                },
                DeathSavingThrows =
                    new DeathSavingThrowState
                    {
                        SuccessCount = 0,
                        FailureCount = 0,
                        IsStable = false
                    },
                IsInstantlyDead = false
            },
            Ammunition = ammunition
        };
    }

    private static IReadOnlyList<ClassDefinition>
        CreateClasses()
    {
        return
        [
            CreateClass(
                WatchtowerPartyDefinitions
                    .FighterClassId,
                "Fighter",
                DieType.D10,
                [
                    Ability.Strength,
                    Ability.Constitution
                ],
                [
                    "skill.athletics",
                    "skill.perception"
                ]),
            CreateClass(
                WatchtowerPartyDefinitions
                    .BarbarianClassId,
                "Barbarian",
                DieType.D12,
                [
                    Ability.Strength,
                    Ability.Constitution
                ],
                [
                    "skill.athletics",
                    "skill.survival"
                ]),
            CreateClass(
                WatchtowerPartyDefinitions
                    .RangerClassId,
                "Ranger",
                DieType.D10,
                [
                    Ability.Strength,
                    Ability.Dexterity
                ],
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
            SavingThrowProficiencies =
                savingThrows,
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

    private static IReadOnlyList<WeaponDefinition>
        CreateWeapons()
    {
        return
        [
            CreateMeleeWeapon(
                WatchtowerPartyDefinitions
                    .FighterWeaponId,
                "Longsword",
                DieType.D8),
            CreateMeleeWeapon(
                WatchtowerPartyDefinitions
                    .BarbarianWeaponId,
                "Greataxe",
                DieType.D12),
            new WeaponDefinition
            {
                Id = WatchtowerPartyDefinitions
                    .RangerWeaponId,
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
                    RuleIds.WeaponProperties
                        .Ammunition,
                    RuleIds.WeaponProperties.Heavy,
                    RuleIds.WeaponProperties
                        .TwoHanded
                ],
                NormalRangeFeet = 150,
                LongRangeFeet = 600,
                AmmunitionItemId =
                    WatchtowerPartyDefinitions
                        .RangerAmmunitionItemId
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
