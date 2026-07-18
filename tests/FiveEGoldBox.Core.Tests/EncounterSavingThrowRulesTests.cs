using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

public sealed class EncounterSavingThrowRulesTests
{
    [Fact]
    public void Resolve_WhenCoverIsOutsideDirectPath_AppliesNoCoverBonus()
    {
        EncounterState state = CreateEncounter(
            coverPositions:
            [
                CreateCoverPosition(
                    new GridPosition(3, 2),
                    EncounterCoverLevel
                        .ThreeQuarters)
            ]);

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(1, 1));

        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .EvaluatedNoCover,
            result.CoverDisposition);
        Assert.NotNull(result.LineOfSight);
        Assert.True(result.LineOfSight.HasLineOfSight);
        Assert.NotNull(result.Cover);
        Assert.Equal(
            EncounterCoverLevel.None,
            result.Cover.CoverLevel);
        Assert.Null(result.Cover.CoverPosition);
        Assert.Equal(0, result.AppliedCoverBonus);
        Assert.Equal(3, result.CombinedSavingThrowBonus);
        Assert.NotNull(result.SavingThrow);
        Assert.Equal(3, result.SavingThrow.Test.Bonus);
    }

    [Fact]
    public void Resolve_WithHalfCover_ChangesFailureToSuccessAndExposesCalculation()
    {
        GridPosition coverPosition =
            new(3, 1);

        EncounterState state = CreateEncounter(
            coverPositions:
            [
                CreateCoverPosition(
                    coverPosition,
                    EncounterCoverLevel.Half)
            ]);

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(1, 1),
                firstRoll: 9,
                difficultyClass: 14);

        Assert.Equal(
            "combatant.target",
            result.TargetCombatantId);
        Assert.Equal(Ability.Dexterity, result.Ability);
        Assert.Equal(
            Ability.Dexterity,
            result.BaseSavingThrowBonus.Ability);
        Assert.Equal(
            3,
            result.BaseSavingThrowBonus.TotalBonus);
        Assert.Equal(
            EncounterSavingThrowCoverPolicy.Permitted,
            result.CoverPolicy);
        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .HalfCoverApplied,
            result.CoverDisposition);
        Assert.NotNull(result.LineOfSight);
        Assert.True(result.LineOfSight.HasLineOfSight);
        Assert.NotNull(result.Cover);
        Assert.Equal(
            EncounterCoverLevel.Half,
            result.Cover.CoverLevel);
        Assert.Equal(
            coverPosition,
            result.Cover.CoverPosition);
        Assert.Equal(2, result.AppliedCoverBonus);
        Assert.Equal(5, result.CombinedSavingThrowBonus);
        Assert.NotNull(result.SavingThrow);
        Assert.Equal(
            Ability.Dexterity,
            result.SavingThrow.Ability);
        Assert.Equal(9, result.SavingThrow.Test.NaturalRoll);
        Assert.Equal(5, result.SavingThrow.Test.Bonus);
        Assert.Equal(14, result.SavingThrow.Test.Total);
        Assert.Equal(
            14,
            result.SavingThrow.Test.DifficultyClass);
        Assert.Equal(
            D20TestOutcome.Success,
            result.SavingThrow.Test.Outcome);
    }

    [Fact]
    public void Resolve_WithThreeQuartersCover_AppliesFivePointBonus()
    {
        EncounterState state = CreateEncounter(
            coverPositions:
            [
                CreateCoverPosition(
                    new GridPosition(3, 1),
                    EncounterCoverLevel
                        .ThreeQuarters)
            ]);

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(1, 1));

        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .ThreeQuartersCoverApplied,
            result.CoverDisposition);
        Assert.NotNull(result.Cover);
        Assert.Equal(
            EncounterCoverLevel.ThreeQuarters,
            result.Cover.CoverLevel);
        Assert.Equal(5, result.AppliedCoverBonus);
        Assert.Equal(8, result.CombinedSavingThrowBonus);
        Assert.NotNull(result.SavingThrow);
        Assert.Equal(8, result.SavingThrow.Test.Bonus);
    }

    [Fact]
    public void Resolve_WhenCoverIsIgnored_AppliesNoCoverAndSkipsGeometry()
    {
        EncounterState state = CreateEncounter(
            coverPositions:
            [
                CreateCoverPosition(
                    new GridPosition(3, 1),
                    EncounterCoverLevel
                        .ThreeQuarters)
            ]);

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                coverPolicy:
                    EncounterSavingThrowCoverPolicy
                        .Ignored,
                originPosition:
                    new GridPosition(1, 1));

        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .IgnoredByEffect,
            result.CoverDisposition);
        Assert.Null(result.LineOfSight);
        Assert.Null(result.Cover);
        Assert.Equal(0, result.AppliedCoverBonus);
        Assert.Equal(3, result.CombinedSavingThrowBonus);
        Assert.NotNull(result.SavingThrow);
        Assert.Equal(3, result.SavingThrow.Test.Bonus);
    }

    [Theory]
    [InlineData(Ability.Strength, 1)]
    [InlineData(Ability.Constitution, 2)]
    [InlineData(Ability.Intelligence, 0)]
    [InlineData(Ability.Wisdom, -1)]
    [InlineData(Ability.Charisma, 4)]
    public void Resolve_WithNonDexterityAbility_DoesNotApplyCover(
        Ability ability,
        int expectedBonus)
    {
        EncounterState state = CreateEncounter(
            coverPositions:
            [
                CreateCoverPosition(
                    new GridPosition(3, 1),
                    EncounterCoverLevel
                        .ThreeQuarters)
            ]);

        EncounterSavingThrowResult result =
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.target",
                ability,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                EncounterSavingThrowCoverPolicy
                    .Permitted,
                originPosition:
                    new GridPosition(1, 1));

        Assert.Equal(ability, result.Ability);
        Assert.Equal(
            ability,
            result.BaseSavingThrowBonus.Ability);
        Assert.Equal(
            expectedBonus,
            result.BaseSavingThrowBonus.TotalBonus);
        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .NotApplicableToAbility,
            result.CoverDisposition);
        Assert.Null(result.LineOfSight);
        Assert.Null(result.Cover);
        Assert.Equal(0, result.AppliedCoverBonus);
        Assert.Equal(
            expectedBonus,
            result.CombinedSavingThrowBonus);
        Assert.NotNull(result.SavingThrow);
        Assert.Equal(
            expectedBonus,
            result.SavingThrow.Test.Bonus);
    }

    [Fact]
    public void Resolve_WhenCoverIsIgnoredAndOriginIsMissing_CompletesSave()
    {
        EncounterState state = CreateEncounter();

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                coverPolicy:
                    EncounterSavingThrowCoverPolicy
                        .Ignored,
                originPosition: null);

        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .NoMeaningfulOrigin,
            result.CoverDisposition);
        Assert.Null(result.LineOfSight);
        Assert.Null(result.Cover);
        Assert.Equal(0, result.AppliedCoverBonus);
        Assert.NotNull(result.SavingThrow);
    }

    [Fact]
    public void Resolve_WhenCoverIsPermittedAndOriginIsMissing_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            ResolveDexteritySavingThrow(
                state,
                originPosition: null));
    }

    [Fact]
    public void Resolve_WhenCoverIsPermittedAndOriginMatchesTarget_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(5, 1)));
    }

    [Fact]
    public void Resolve_WhenTargetIsNotParticipant_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.missing",
                Ability.Dexterity,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                EncounterSavingThrowCoverPolicy
                    .Permitted,
                originPosition:
                    new GridPosition(1, 1)));
    }

    [Fact]
    public void Resolve_WhenRequiredOriginIsOutsideBattlefield_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(-1, 1)));
    }

    [Fact]
    public void Resolve_WhenDirectPathIsBlocked_ReturnsBlockingInformationWithoutSave()
    {
        GridPosition blockingPosition =
            new(3, 1);

        EncounterState state = CreateEncounter(
            blockedPositions:
            [
                blockingPosition
            ]);

        EncounterSavingThrowResult result =
            ResolveDexteritySavingThrow(
                state,
                originPosition:
                    new GridPosition(1, 1));

        Assert.Equal(
            EncounterSavingThrowCoverDisposition
                .DirectPathBlocked,
            result.CoverDisposition);
        Assert.NotNull(result.LineOfSight);
        Assert.False(result.LineOfSight.HasLineOfSight);
        Assert.Equal(
            blockingPosition,
            result.LineOfSight.BlockingPosition);
        Assert.Null(result.Cover);
        Assert.Null(result.AppliedCoverBonus);
        Assert.Null(result.CombinedSavingThrowBonus);
        Assert.Null(result.SavingThrow);
    }

    [Fact]
    public void Resolve_WhenRequestedSavingThrowBonusIsMissing_Throws()
    {
        EncounterState state = CreateEncounter(
            targetSavingThrowBonuses:
            [
                CreateSavingThrowBonus(
                    Ability.Dexterity,
                    totalBonus: 3)
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.target",
                Ability.Wisdom,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                EncounterSavingThrowCoverPolicy
                    .Ignored,
                originPosition: null));
    }

    [Fact]
    public void Resolve_WithUnsupportedAbility_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.target",
                (Ability)999,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                EncounterSavingThrowCoverPolicy
                    .Ignored,
                originPosition: null));
    }

    [Fact]
    public void Resolve_WithUnsupportedRollMode_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.target",
                Ability.Dexterity,
                (D20RollMode)999,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                EncounterSavingThrowCoverPolicy
                    .Ignored,
                originPosition: null));
    }

    [Fact]
    public void Resolve_WithUnsupportedCoverPolicy_Throws()
    {
        EncounterState state = CreateEncounter();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncounterSavingThrowRules.Resolve(
                state,
                targetCombatantId:
                    "combatant.target",
                Ability.Dexterity,
                D20RollMode.Normal,
                firstRoll: 10,
                secondRoll: null,
                difficultyClass: 12,
                (EncounterSavingThrowCoverPolicy)999,
                originPosition: null));
    }

    private static EncounterSavingThrowResult
        ResolveDexteritySavingThrow(
            EncounterState state,
            EncounterSavingThrowCoverPolicy coverPolicy =
                EncounterSavingThrowCoverPolicy.Permitted,
            GridPosition? originPosition = null,
            int firstRoll = 10,
            int difficultyClass = 12)
    {
        return EncounterSavingThrowRules.Resolve(
            state,
            targetCombatantId:
                "combatant.target",
            Ability.Dexterity,
            D20RollMode.Normal,
            firstRoll,
            secondRoll: null,
            difficultyClass,
            coverPolicy,
            originPosition);
    }

    private static EncounterState CreateEncounter(
        IReadOnlyList<GridPosition>?
            blockedPositions = null,
        IReadOnlyList<EncounterCoverPosition>?
            coverPositions = null,
        IReadOnlyList<SavingThrowBonus>?
            targetSavingThrowBonuses = null)
    {
        EncounterParticipantSetup[] participants =
        [
            CreateParticipant(
                combatantId: "combatant.target",
                sideId: "side.party",
                position: new GridPosition(5, 1),
                savingThrowBonuses:
                    targetSavingThrowBonuses
                    ?? CreateAllSavingThrowBonuses()),
            CreateParticipant(
                combatantId: "combatant.source",
                sideId: "side.enemies",
                position: new GridPosition(1, 3),
                savingThrowBonuses:
                    Array.Empty<SavingThrowBonus>())
        ];

        InitiativeOrderEntry[] initiativeOrder =
        [
            CreateInitiativeEntry(
                "combatant.target",
                position: 1,
                total: 15),
            CreateInitiativeEntry(
                "combatant.source",
                position: 2,
                total: 10)
        ];

        return EncounterRules.Start(
            encounterId: "encounter.test",
            new EncounterBattlefieldState
            {
                BattlefieldId = "battlefield.test",
                Width = 8,
                Height = 4,
                BlockedPositions =
                    blockedPositions
                    ?? Array.Empty<GridPosition>(),
                CoverPositions =
                    coverPositions
                    ?? Array.Empty<
                        EncounterCoverPosition>(),
                DifficultTerrainPositions =
                    Array.Empty<GridPosition>()
            },
            participants,
            initiativeOrder);
    }

    private static EncounterParticipantSetup
        CreateParticipant(
            string combatantId,
            string sideId,
            GridPosition position,
            IReadOnlyList<SavingThrowBonus>
                savingThrowBonuses)
    {
        return new EncounterParticipantSetup
        {
            Combatant = CombatantRules.Create(
                combatantId,
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 10,
                SavingThrowBonuses =
                    savingThrowBonuses
            },
            SideId = sideId,
            MovementSpeedFeet = 30,
            StartingPosition = position
        };
    }

    private static IReadOnlyList<SavingThrowBonus>
        CreateAllSavingThrowBonuses()
    {
        return
        [
            CreateSavingThrowBonus(
                Ability.Strength,
                totalBonus: 1),
            CreateSavingThrowBonus(
                Ability.Dexterity,
                totalBonus: 3),
            CreateSavingThrowBonus(
                Ability.Constitution,
                totalBonus: 2),
            CreateSavingThrowBonus(
                Ability.Intelligence,
                totalBonus: 0),
            CreateSavingThrowBonus(
                Ability.Wisdom,
                totalBonus: -1),
            CreateSavingThrowBonus(
                Ability.Charisma,
                totalBonus: 4)
        ];
    }

    private static SavingThrowBonus
        CreateSavingThrowBonus(
            Ability ability,
            int totalBonus)
    {
        return new SavingThrowBonus
        {
            Ability = ability,
            AbilityModifier = totalBonus,
            IsProficient = false,
            ProficiencyBonus = 0,
            TotalBonus = totalBonus
        };
    }

    private static EncounterCoverPosition
        CreateCoverPosition(
            GridPosition position,
            EncounterCoverLevel coverLevel)
    {
        return new EncounterCoverPosition
        {
            Position = position,
            CoverLevel = coverLevel
        };
    }

    private static InitiativeOrderEntry
        CreateInitiativeEntry(
            string combatantId,
            int position,
            int total)
    {
        return new InitiativeOrderEntry
        {
            CombatantId = combatantId,
            Initiative = InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                firstRoll: total,
                secondRoll: null,
                initiativeBonus: 0),
            Position = position,
            HasTiedInitiative = false
        };
    }
}
