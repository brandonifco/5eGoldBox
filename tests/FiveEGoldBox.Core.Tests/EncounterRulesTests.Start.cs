using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Starting an encounter: participant setup, initiative, battlefield
/// placement, and everything Start rejects.
public sealed partial class EncounterRulesTests
{
    [Fact]
    public void Start_WithValidParticipants_CreatesActiveEncounter()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            movementSpeedFeet: 30),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies",
            movementSpeedFeet: 25)
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 2,
            total: 10)
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        Assert.Equal("encounter.test", state.EncounterId);
        Assert.Equal(1, state.Revision);
        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Equal(2, state.Participants.Count);
        Assert.Equal(2, state.InitiativeOrder.Count);
        Assert.Equal(1, state.TurnState.RoundNumber);
        Assert.Equal(1, state.TurnState.ActivePosition);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);

        Assert.True(
            state.Participants[0]
                .TurnResources.HasActionAvailable);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasBonusActionAvailable);
        Assert.True(
            state.Participants[0]
                .TurnResources.HasReactionAvailable);
        Assert.Equal(
            30,
            state.Participants[0]
                .TurnResources.MovementSpeedFeet);
        Assert.Equal(
            0,
            state.Participants[0]
                .TurnResources.MovementSpentFeet);
        Assert.Equal(
            30,
            state.Participants[0]
                .TurnResources.MovementRemainingFeet);

        Assert.Equal(
            25,
            state.Participants[1]
                .TurnResources.MovementSpeedFeet);
        Assert.Equal(
            25,
            state.Participants[1]
                .TurnResources.MovementRemainingFeet);
    }

    [Fact]
    public void Start_WhenOnlyOneSideIsOutcomeViable_CompletesWithWinningSide()
    {
        CombatantState dyingEnemy =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId:
                        "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        CombatantState stableEnemy =
            dyingEnemy with
            {
                Health = dyingEnemy.Health with
                {
                    DeathSavingThrows =
                        DeathSavingThrowRules.Create() with
                        {
                            IsStable = true
                        }
                }
            };

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                movementSpeedFeet: 30),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies",
                movementSpeedFeet: 25) with
            {
                Combatant = stableEnemy
            }
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            CreateInitiativeOrder());

        Assert.Equal(
            EncounterLifecycleState.Completed,
            state.LifecycleState);
        Assert.Equal(
            "side.party",
            state.WinningSideId);
        Assert.Equal(1, state.Revision);
        Assert.Null(
            state.PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Start_WhenOnlyViableSideHasDyingCombatant_RemainsActive()
    {
        CombatantState dyingAlly =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId:
                        "combatant.ally",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        CombatantState dyingEnemy =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId:
                        "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy
                        .DeathSavingThrows),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        CombatantState stableEnemy =
            dyingEnemy with
            {
                Health = dyingEnemy.Health with
                {
                    DeathSavingThrows =
                        DeathSavingThrowRules.Create() with
                        {
                            IsStable = true
                        }
                }
            };

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party",
                movementSpeedFeet: 30,
                startingPosition:
                    new GridPosition(1, 1)),
            CreateParticipant(
                combatantId: "combatant.ally",
                sideId: "side.party",
                movementSpeedFeet: 30,
                startingPosition:
                    new GridPosition(1, 2)) with
            {
                Combatant = dyingAlly
            },
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies",
                movementSpeedFeet: 25,
                startingPosition:
                    new GridPosition(2, 1)) with
            {
                Combatant = stableEnemy
            }
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15),
            CreateInitiativeEntry(
                combatantId: "combatant.ally",
                position: 2,
                total: 12),
            CreateInitiativeEntry(
                combatantId: "combatant.enemy",
                position: 3,
                total: 10)
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        Assert.Equal(
            EncounterLifecycleState.Active,
            state.LifecycleState);
        Assert.Null(state.WinningSideId);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            state.Participants[1]
                .Combatant.LifecycleState);
        Assert.Null(
            state.PendingDeathSavingThrowCombatantId);
    }

    [Fact]
    public void Start_WithUnorderedInitiativeEntries_OrdersByPosition()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party"),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies")
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                combatantId: "combatant.enemy",
                position: 2,
                total: 10),
            CreateInitiativeEntry(
                combatantId: "combatant.hero",
                position: 1,
                total: 15)
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            initiativeOrder);

        Assert.Equal(
            "combatant.hero",
            state.InitiativeOrder[0].CombatantId);
        Assert.Equal(
            "combatant.enemy",
            state.InitiativeOrder[1].CombatantId);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Fact]
    public void Start_ProtectsParticipantsFromSourceCollectionMutation()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.hero",
                sideId: "side.party"),
            CreateParticipant(
                combatantId: "combatant.enemy",
                sideId: "side.enemies")
        ];

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            CreateInitiativeOrder());

        participants[0] = CreateParticipant(
            combatantId: "combatant.replacement",
            sideId: "side.party");

        Assert.Equal(
            "combatant.hero",
            state.Participants[0].Combatant.CombatantId);
    }

    [Fact]
    public void Start_ProtectsInitiativeOrderFromSourceCollectionMutation()
    {
        InitiativeOrderEntry[] initiativeOrder =
            CreateInitiativeOrder();

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            CreateParticipants(),
            initiativeOrder);

        initiativeOrder[0] = CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 1,
            total: 10);

        Assert.Equal(
            "combatant.hero",
            state.InitiativeOrder[0].CombatantId);
        Assert.Equal(
            "combatant.hero",
            state.ActiveCombatantId);
    }

    [Fact]
    public void Start_WithSavingThrowBonuses_StoresProtectedCollection()
    {
        SavingThrowBonus[] savingThrowBonuses =
        [
            CreateSavingThrowBonus(
                Ability.Dexterity,
                abilityModifier: 1,
                isProficient: true,
                proficiencyBonus: 2,
                totalBonus: 3)
        ];

        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    SavingThrowBonuses =
                        savingThrowBonuses
                }
        };

        EncounterState state = StartEncounter(
            encounterId: "encounter.test",
            participants,
            CreateInitiativeOrder());

        savingThrowBonuses[0] =
            CreateSavingThrowBonus(
                Ability.Wisdom,
                abilityModifier: -2,
                isProficient: false,
                proficiencyBonus: 0,
                totalBonus: -2);

        SavingThrowBonus storedBonus =
            Assert.Single(
                state.Participants[0]
                    .CombatProfile
                    .SavingThrowBonuses);

        Assert.Equal(
            Ability.Dexterity,
            storedBonus.Ability);
        Assert.Equal(1, storedBonus.AbilityModifier);
        Assert.True(storedBonus.IsProficient);
        Assert.Equal(2, storedBonus.ProficiencyBonus);
        Assert.Equal(3, storedBonus.TotalBonus);
    }

    [Fact]
    public void Start_WithNullSavingThrowBonusCollection_Throws()
    {
        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    SavingThrowBonuses = null!
                }
        };

        Assert.Throws<ArgumentNullException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithNullSavingThrowBonusEntry_Throws()
    {
        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    SavingThrowBonuses =
                    [
                        null!
                    ]
                }
        };

        Assert.Throws<ArgumentNullException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithDuplicateSavingThrowAbilities_Throws()
    {
        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    SavingThrowBonuses =
                    [
                        CreateSavingThrowBonus(
                            Ability.Dexterity,
                            totalBonus: 3),
                        CreateSavingThrowBonus(
                            Ability.Dexterity,
                            totalBonus: 5)
                    ]
                }
        };

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithUnsupportedSavingThrowAbility_Throws()
    {
        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    SavingThrowBonuses =
                    [
                        CreateSavingThrowBonus(
                            (Ability)999,
                            totalBonus: 3)
                    ]
                }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Start_WithNonpositiveArmorClass_Throws(
    int armorClass)
    {
        EncounterParticipantSetup[] participants =
            CreateParticipants();

        participants[0] = participants[0] with
        {
            CombatProfile =
                participants[0].CombatProfile with
                {
                    ArmorClass = armorClass
                }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Start_WithBlankEncounterId_Throws(
    string encounterId)
    {
        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithNoParticipants_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants:
                    Array.Empty<EncounterParticipantSetup>(),
                initiativeOrder:
                    Array.Empty<InitiativeOrderEntry>()));
    }

    [Fact]
    public void Start_WithOnlyOneSide_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero.one",
            sideId: "side.party",
            startingPosition:
                new GridPosition(1, 1)),
        CreateParticipant(
            combatantId: "combatant.hero.two",
            sideId: "side.party",
            startingPosition:
                new GridPosition(2, 1))
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero.one",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.hero.two",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateCombatantIds_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.duplicate",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.duplicate",
            sideId: "side.enemies")
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithBlankSideId_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: " ")
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithTerminalCombatant_Throws()
    {
        CombatantState defeatedCombatant =
            CombatantRules.ResolveDamage(
                CombatantRules.Create(
                    combatantId: "combatant.enemy",
                    maximumHitPoints: 10,
                    CombatantZeroHitPointPolicy.Defeated),
                damageAmount: 10,
                isCriticalHit: false)
            .State;

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party"),
        new EncounterParticipantSetup
        {
            Combatant = defeatedCombatant,
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10
            },
            SideId = "side.enemies",
            MovementSpeedFeet = 30,
            StartingPosition = new GridPosition(2, 1)
        }

        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WhenInitiativeCountDoesNotMatchParticipants_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateInitiativeCombatantIds_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithInitiativeCombatantOutsideEncounter_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.outsider",
            position: 2,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithDuplicateInitiativePositions_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 1,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithNoncontiguousInitiativePositions_Throws()
    {
        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
            combatantId: "combatant.hero",
            position: 1,
            total: 15),
        CreateInitiativeEntry(
            combatantId: "combatant.enemy",
            position: 3,
            total: 10)
        ];

        Assert.Throws<ArgumentException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                CreateParticipants(),
                initiativeOrder));
    }

    [Fact]
    public void Start_WithNegativeMovementSpeed_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            movementSpeedFeet: -1),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies")
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StartEncounter(
                encounterId: "encounter.test",
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithValidBattlefield_StoresBattlefieldAndPositions()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    new GridPosition(5, 5)
                ],
                difficultTerrainPositions:
                [
                    new GridPosition(3, 3)
                ]);

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            battlefield,
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Equal(
            "battlefield.test",
            state.Battlefield.BattlefieldId);
        Assert.Equal(12, state.Battlefield.Width);
        Assert.Equal(12, state.Battlefield.Height);
        Assert.Contains(
            new GridPosition(5, 5),
            state.Battlefield.BlockedPositions);
        Assert.Contains(
            new GridPosition(3, 3),
            state.Battlefield.DifficultTerrainPositions);
        Assert.Equal(
            new GridPosition(1, 1),
            state.Participants[0].Position);
        Assert.Equal(
            new GridPosition(2, 1),
            state.Participants[1].Position);
    }

    [Fact]
    public void Start_ProtectsBattlefieldTerrainFromSourceCollectionMutation()
    {
        GridPosition[] blockedPositions =
        [
            new GridPosition(5, 5)
        ];

        GridPosition[] difficultTerrainPositions =
        [
            new GridPosition(3, 3)
        ];

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions: blockedPositions,
                difficultTerrainPositions:
                    difficultTerrainPositions);

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            battlefield,
            CreateParticipants(),
            CreateInitiativeOrder());

        blockedPositions[0] =
            new GridPosition(6, 6);
        difficultTerrainPositions[0] =
            new GridPosition(4, 4);

        Assert.Equal(
            new GridPosition(5, 5),
            Assert.Single(
                state.Battlefield.BlockedPositions));
        Assert.Equal(
            new GridPosition(3, 3),
            Assert.Single(
                state.Battlefield
                    .DifficultTerrainPositions));
    }

    [Fact]
    public void Start_ProtectsBattlefieldCoverFromSourceCollectionMutation()
    {
        EncounterCoverPosition[] coverPositions =
        [
            new EncounterCoverPosition
            {
                Position = new GridPosition(4, 4),
                CoverLevel = EncounterCoverLevel.Half
            }
        ];

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                coverPositions: coverPositions);

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            battlefield,
            CreateParticipants(),
            CreateInitiativeOrder());

        coverPositions[0] =
            new EncounterCoverPosition
            {
                Position = new GridPosition(6, 6),
                CoverLevel =
                    EncounterCoverLevel.ThreeQuarters
            };

        EncounterCoverPosition storedCover =
            Assert.Single(
                state.Battlefield.CoverPositions);

        Assert.Equal(
            new GridPosition(4, 4),
            storedCover.Position);
        Assert.Equal(
            EncounterCoverLevel.Half,
            storedCover.CoverLevel);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Start_WithBlankBattlefieldId_Throws(
        string battlefieldId)
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                battlefieldId: battlefieldId);

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Theory]
    [InlineData(0, 12)]
    [InlineData(-1, 12)]
    [InlineData(12, 0)]
    [InlineData(12, -1)]
    public void Start_WithNonpositiveBattlefieldDimension_Throws(
        int width,
        int height)
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                width: width,
                height: height);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithDuplicateBlockedPositions_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    new GridPosition(5, 5),
                new GridPosition(5, 5)
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithDuplicateDifficultTerrainPositions_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                difficultTerrainPositions:
                [
                    new GridPosition(3, 3),
                new GridPosition(3, 3)
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithBlockedPositionOutsideBattlefield_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    new GridPosition(12, 0)
                ]);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithDifficultTerrainOutsideBattlefield_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                difficultTerrainPositions:
                [
                    new GridPosition(-1, 0)
                ]);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithPositionBothBlockedAndDifficult_Throws()
    {
        GridPosition position =
            new(3, 3);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    position
                ],
                difficultTerrainPositions:
                [
                    position
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithParticipantOutsideBattlefield_Throws()
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            startingPosition:
                new GridPosition(12, 0)),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies")
        ];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateBattlefield(),
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithParticipantOnBlockedPosition_Throws()
    {
        GridPosition blockedPosition =
            new(1, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                blockedPositions:
                [
                    blockedPosition
                ]);

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithParticipantsSharingPosition_Throws()
    {
        GridPosition sharedPosition =
            new(1, 1);

        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
            combatantId: "combatant.hero",
            sideId: "side.party",
            startingPosition: sharedPosition),
        CreateParticipant(
            combatantId: "combatant.enemy",
            sideId: "side.enemies",
            startingPosition: sharedPosition)
        ];

        Assert.Throws<ArgumentException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                CreateBattlefield(),
                participants,
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithParticipantOnDifficultTerrain_AcceptsPosition()
    {
        GridPosition difficultPosition =
            new(1, 1);

        EncounterBattlefieldState battlefield =
            CreateBattlefield(
                difficultTerrainPositions:
                [
                    difficultPosition
                ]);

        EncounterState state = EncounterRules.Start(
            encounterId: "encounter.test",
            battlefield,
            CreateParticipants(),
            CreateInitiativeOrder());

        Assert.Equal(
            difficultPosition,
            state.Participants[0].Position);
    }

    [Fact]
    public void Start_WithNullBlockedPositions_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield() with
            {
                BlockedPositions = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }

    [Fact]
    public void Start_WithNullDifficultTerrainPositions_Throws()
    {
        EncounterBattlefieldState battlefield =
            CreateBattlefield() with
            {
                DifficultTerrainPositions = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            EncounterRules.Start(
                encounterId: "encounter.test",
                battlefield,
                CreateParticipants(),
                CreateInitiativeOrder()));
    }
}
