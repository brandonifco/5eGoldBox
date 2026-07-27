using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Direct tests for the player command resolver extracted from
/// WatchtowerCombatOrchestrator. Resolving a command needs only encounter state,
/// so these run without a session or automatic processing — which is what the
/// extraction buys. End-to-end command behaviour stays covered by
/// WatchtowerCombatExecutionTests and the characterization transcripts.
public sealed class WatchtowerPlayerCommandResolverTests
{
    [Fact]
    public void Resolve_Move_ReportsMovementStepAndConsumesNoRandomness()
    {
        Fixture fixture = Fixture.Create();
        WatchtowerCombatMovementDestinationOption destination =
            Assert.IsType<WatchtowerCombatMovementOption>(
                fixture.Decision.Movement)
            .DestinationOptions[0];

        WatchtowerPlayerCommandResolution resolution =
            WatchtowerPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    Path = destination.Path
                });

        Assert.Equal(fixture.CursorBefore, resolution.CursorAfter);
        Assert.Equal(
            WatchtowerCombatStepKind.Movement,
            resolution.PrimaryStep.Kind);
        Assert.Empty(resolution.PrimaryStep.Dice);
        Assert.Equal(WatchtowerCombatIntentKind.Move, resolution.Receipt.Kind);
        Assert.Equal(destination.Path, resolution.Receipt.Path);
        Assert.True(resolution.State.Revision > fixture.Revision);
    }

    [Fact]
    public void Resolve_EndTurn_ReportsPlayerEndTurnAndConsumesNoRandomness()
    {
        Fixture fixture = Fixture.Create();

        WatchtowerPlayerCommandResolution resolution =
            WatchtowerPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId
                });

        Assert.Equal(fixture.CursorBefore, resolution.CursorAfter);
        Assert.Equal(
            WatchtowerCombatStepKind.TurnAdvanced,
            resolution.PrimaryStep.Kind);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.PlayerEndTurn,
            resolution.PrimaryStep.TurnAdvanceReason);
        Assert.Equal(
            WatchtowerCombatIntentKind.EndTurn,
            resolution.Receipt.Kind);
        Assert.Empty(resolution.Receipt.Path);
    }

    /// The cursor a resolution reports must account for exactly the dice its
    /// step recorded, otherwise replaying a save would desynchronise.
    [Fact]
    public void Resolve_WeaponAttack_AdvancesCursorByExactlyTheDiceItRecorded()
    {
        Fixture fixture = Fixture.Create();
        WatchtowerCombatWeaponAttackOption weapon =
            fixture.Decision.WeaponAttacks.Single();
        WatchtowerCombatTargetOption target = Assert.Single(
            weapon.Targets,
            candidate => candidate.IsAvailable);

        WatchtowerPlayerCommandResolution resolution =
            WatchtowerPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    WeaponId = weapon.WeaponId,
                    TargetCombatantId = target.TargetCombatantId
                });

        Assert.Equal(
            WatchtowerCombatStepKind.WeaponAttack,
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
            WatchtowerPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new WatchtowerCombatMoveIntent
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
            WatchtowerPlayerCommandResolver.Resolve(
                fixture.Encounter,
                fixture.RandomSeed,
                fixture.CursorBefore,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = fixture.Revision,
                    ActorCombatantId = fixture.ActorId,
                    WeaponId = blank,
                    TargetCombatantId = "combatant.watchtower-raider.melee"
                }));
    }

    private sealed record Fixture(
        EncounterState Encounter,
        WatchtowerCombatDecision Decision,
        string ActorId,
        long Revision,
        int RandomSeed,
        int CursorBefore)
    {
        internal static Fixture Create()
        {
            ApplicationSessionState state =
                WatchtowerCombatTestData.CreatePlayerDecisionSession();
            WatchtowerCombatDecision decision =
                WatchtowerCombatRules.AdvanceToDecision(state).ResultingDecision;
            EncounterState encounter =
                WatchtowerCombatTestData.GetEncounter(state);

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
