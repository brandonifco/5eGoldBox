using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Scenarios;

/// The rules the campaign plays by.
///
/// This is campaign content rather than any one scenario's: it holds the
/// classes, races and skills the party is built from, and the gear both the
/// party and every scenario's opposition draw on. Two scenarios share it
/// because one campaign means one body of rules.
internal static partial class CampaignRulesetContent
{
    private const string HumanRaceId =
        "race.human";

    private const string SoldierBackgroundId =
        "background.soldier";

    /// Exposed unloaded so tests can derive variants from the authored
    /// ruleset rather than keeping a parallel copy of it, which is how the
    /// raiders' weapons went missing from one and not the other.
    internal static ValidatedRuleset Load(
        RulesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        RulesetLoadResult load =
            ApplicationRulesetLoader.Load(definition);

        if (!load.IsValid || load.Ruleset is null)
        {
            throw new InvalidOperationException(
                "The campaign ruleset could not be validated: "
                    + string.Join(
                        "; ",
                        load.Validation.Issues
                            .Where(issue =>
                                issue.Severity == ValidationSeverity.Error)
                            .Select(issue => $"{issue.Code} {issue.Message}")));
        }

        return load.Ruleset;
    }

    internal static RulesetDefinition CreateRulesetDefinition()
    {
        return new RulesetDefinition
        {
            Id = RulesetRegistry.CampaignRulesetId,
            Name = "Campaign Ruleset",
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
            Spells = CreateSpells(),
            Effects = CreateEffects(),
            EquipmentItems =
            [
                new EquipmentItemDefinition
                {
                    Id = CampaignRulesetContent
                        .RangerAmmunitionItemId,
                    Name = "Arrow",
                    WeightPounds = 0.05m
                }
            ]
        };
    }

    private static IReadOnlyList<ClassDefinition>
        CreateClasses()
    {
        return
        [
            CreateClass(
                CampaignRulesetContent
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
                CampaignRulesetContent
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
            CreateCaster(
                ClericClassId,
                "Cleric",
                DieType.D8,
                Ability.Wisdom,
                [
                    Ability.Wisdom,
                    Ability.Charisma
                ],
                [
                    "skill.perception",
                    "skill.survival"
                ]),
            CreateCaster(
                WizardClassId,
                "Wizard",
                DieType.D6,
                Ability.Intelligence,
                [
                    Ability.Intelligence,
                    Ability.Wisdom
                ],
                [
                    "skill.perception",
                    "skill.stealth"
                ]),
            CreateClass(
                CampaignRulesetContent
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

    /// A class that casts. Every caster in the campaign is level one, so all
    /// of them have the same two first-level slots; what differs is the
    /// ability they cast with.
    private static ClassDefinition CreateCaster(
        string id,
        string name,
        DieType hitDie,
        Ability spellcastingAbility,
        IReadOnlyList<Ability> savingThrows,
        IReadOnlyList<string> skills)
    {
        return CreateClass(id, name, hitDie, savingThrows, skills) with
        {
            SpellcastingAbility = spellcastingAbility,
            SpellSlotsByLevel = new Dictionary<int, int>
            {
                [1] = 2
            }
        };
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
                CampaignRulesetContent
                    .FighterWeaponId,
                "Longsword",
                DieType.D8),
            CreateMeleeWeapon(
                CampaignRulesetContent
                    .BarbarianWeaponId,
                "Greataxe",
                DieType.D12),
            new WeaponDefinition
            {
                Id = CampaignRulesetContent
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
                    CampaignRulesetContent
                        .RangerAmmunitionItemId
            },
            // The raiders' gear. It lives in the ruleset like everyone
            // else's now that the scenario's combatants resolve their
            // weapons from it rather than carrying hand-built profiles.
            new WeaponDefinition
            {
                Id = WatchtowerSignalEncounter
                    .MeleeRaiderWeaponId,
                Name = "Raider Scimitar",
                Category = WeaponCategory.Martial,
                AttackKind = WeaponAttackKind.Melee,
                Damage = new DamageDice
                {
                    Count = 1,
                    Die = DieType.D6
                },
                DamageType = "damage.slashing",
                Properties =
                [
                    RuleIds.WeaponProperties.Finesse
                ],
                ReachFeet = 5
            },
            new WeaponDefinition
            {
                Id = WatchtowerSignalEncounter
                    .RangedRaiderWeaponId,
                Name = "Raider Shortbow",
                Category = WeaponCategory.Simple,
                AttackKind = WeaponAttackKind.Ranged,
                Damage = new DamageDice
                {
                    Count = 1,
                    Die = DieType.D6
                },
                DamageType = "damage.piercing",
                Properties =
                [
                    RuleIds.WeaponProperties.Ammunition
                ],
                NormalRangeFeet = 80,
                LongRangeFeet = 320,
                AmmunitionItemId = WatchtowerSignalEncounter
                    .RangedRaiderAmmunitionItemId
            },
            // The chapel guardians' gear. Its dagger is the campaign's first
            // d4 weapon, which only became rollable in Phase 7.
            new WeaponDefinition
            {
                Id = SunkenChapelScenarioDefinitionProvider
                    .GuardianDaggerId,
                Name = "Barnacled Dagger",
                Category = WeaponCategory.Simple,
                AttackKind = WeaponAttackKind.Melee,
                Damage = new DamageDice
                {
                    Count = 1,
                    Die = DieType.D4
                },
                DamageType = "damage.piercing",
                Properties =
                [
                    RuleIds.WeaponProperties.Finesse
                ],
                ReachFeet = 5
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
