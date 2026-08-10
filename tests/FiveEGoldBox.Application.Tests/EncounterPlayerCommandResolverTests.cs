using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Direct tests for the player command resolver extracted from
/// EncounterCombatOrchestrator. Resolving a command needs only encounter state,
/// so these run without a session or automatic processing — which is what the
/// extraction buys. End-to-end command behaviour stays covered by
/// EncounterCombatExecutionTests and the characterization transcripts.
public sealed class EncounterPlayerCommandResolverTests
{
    [Fact]
    public void Resolve_Move_ReportsMovementStepAndConsumesNoRandomness()
    {
        Fixture fixture = Fixture.Create();
        EncounterCombatMovementDestinationOption destination =
            Assert.IsType<EncounterCombatMovementOption>(
                fixture.Decision.Movement)
            .DestinationOptions[0];

        EncounterPlayerCommandResolution resolution =
            EncounterPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new CombatMoveIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    Path = destination.Path
                });

        Assert.Equal(fixture.CursorBefore, resolution.CursorAfter);
        Assert.Equal(
            CombatStepKind.Movement,
            resolution.PrimaryStep.Kind);
        Assert.Empty(resolution.PrimaryStep.Dice);
        Assert.Equal(CombatIntentKind.Move, resolution.Receipt.Kind);
        Assert.Equal(destination.Path, resolution.Receipt.Path);
        Assert.True(resolution.State.Revision > fixture.Revision);
    }

    [Fact]
    public void Resolve_EndTurn_ReportsPlayerEndTurnAndConsumesNoRandomness()
    {
        Fixture fixture = Fixture.Create();

        EncounterPlayerCommandResolution resolution =
            EncounterPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new CombatEndTurnIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId
                });

        Assert.Equal(fixture.CursorBefore, resolution.CursorAfter);
        Assert.Equal(
            CombatStepKind.TurnAdvanced,
            resolution.PrimaryStep.Kind);
        Assert.Equal(
            EncounterCombatTurnAdvanceReason.PlayerEndTurn,
            resolution.PrimaryStep.TurnAdvanceReason);
        Assert.Equal(
            CombatIntentKind.EndTurn,
            resolution.Receipt.Kind);
        Assert.Empty(resolution.Receipt.Path);
    }

    /// The cursor a resolution reports must account for exactly the dice its
    /// step recorded, otherwise replaying a save would desynchronise.
    [Fact]
    public void Resolve_WeaponAttack_AdvancesCursorByExactlyTheDiceItRecorded()
    {
        Fixture fixture = Fixture.Create();
        EncounterCombatWeaponAttackOption weapon =
            fixture.Decision.WeaponAttacks.Single();
        EncounterCombatTargetOption target = Assert.Single(
            weapon.Targets,
            candidate => candidate.IsAvailable);

        EncounterPlayerCommandResolution resolution =
            EncounterPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new CombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    WeaponId = weapon.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(
            CombatStepKind.WeaponAttack,
            resolution.PrimaryStep.Kind);
        Assert.NotEmpty(resolution.PrimaryStep.Dice);
        Assert.Equal(
            fixture.CursorBefore + resolution.PrimaryStep.Dice.Count,
            resolution.CursorAfter);
        Assert.Equal(
            fixture.CursorBefore,
            resolution.PrimaryStep.Dice[0].Ordinal - 1);
    }

    [Fact]
    public void Resolve_EmptyMovementPath_IsRejected()
    {
        Fixture fixture = Fixture.Create();

        Assert.Throws<ArgumentException>(() =>
            EncounterPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new CombatMoveIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    Path = Array.Empty<GridPosition>()
                }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankAttackIdentifiers_AreRejected(string blank)
    {
        Fixture fixture = Fixture.Create();

        Assert.Throws<ArgumentException>(() =>
            EncounterPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new CombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    WeaponId = blank,
                    TargetCombatantId = "combatant.watchtower-raider.melee"
                }));
    }

    private sealed record Fixture(
        EncounterState Encounter,
        EncounterCombatDecision Decision,
        string ActorId,
        long Revision,
        int RandomSeed,
        int CursorBefore)
    {
        internal static Fixture Create()
        {
            ApplicationSessionState state =
                EncounterCombatTestData.CreatePlayerDecisionSession();
            EncounterCombatDecision decision =
                EncounterCombatRules.AdvanceToDecision(state).ResultingDecision;
            EncounterState encounter =
                EncounterCombatTestData.GetEncounter(state);

            return new Fixture(
                encounter,
                decision,
                Assert.IsType<string>(decision.ActiveCombatantId),
                encounter.Revision,
                state.RandomSeed,
                state.RandomValuesConsumed);
        }
    }
}
