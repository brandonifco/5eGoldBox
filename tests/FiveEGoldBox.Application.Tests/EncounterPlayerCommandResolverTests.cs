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
    /// Movement itself rolls nothing — the dice a move can now consume all
    /// belong to opportunity attacks it provoked, never to the movement.
    /// Hence a destination chosen specifically for provoking nothing: the
    /// Watchtower ambush starts the party in contact, so plenty of its
    /// legal destinations do provoke, and picking one arbitrarily would
    /// make this assert the opposite of what it means on some fixtures and
    /// not others.
    [Fact]
    public void Resolve_Move_ReportsMovementStepAndConsumesNoRandomness()
    {
        Fixture fixture = Fixture.Create();
        EncounterCombatMovementDestinationOption destination =
            FindDestination(fixture, provoking: false);

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
        // PrimaryStep is nullable now that an opportunity attack can stop a
        // move before its first square; this path provokes nothing, so it
        // is still a real movement step.
        Assert.NotNull(resolution.PrimaryStep);
        Assert.Equal(
            CombatStepKind.Movement,
            resolution.PrimaryStep.Kind);
        Assert.Empty(resolution.PrimaryStep.Dice);
        Assert.Empty(resolution.ReactionSteps);
        Assert.Equal(CombatIntentKind.Move, resolution.Receipt.Kind);
        Assert.Equal(destination.Path, resolution.Receipt.Path);
        Assert.True(resolution.State.Revision > fixture.Revision);
    }

    /// The other half of the same fact: walking out of an enemy's reach
    /// costs a free attack, and the dice for it are drawn against the
    /// caller's own cursor rather than appearing from nowhere.
    [Fact]
    public void Resolve_Move_OutOfReach_ProvokesAnOpportunityAttack()
    {
        Fixture fixture = Fixture.Create();
        EncounterCombatMovementDestinationOption destination =
            FindDestination(fixture, provoking: true);

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

        EncounterCombatStepResult reaction =
            Assert.Single(resolution.ReactionSteps);

        Assert.Equal(CombatStepKind.WeaponAttack, reaction.Kind);
        Assert.True(reaction.IsOpportunityAttack);
        Assert.Equal(fixture.ActorId, reaction.TargetCombatantId);
        Assert.NotEmpty(reaction.Dice);
        Assert.Equal(
            fixture.CursorBefore + reaction.Dice.Count,
            resolution.CursorAfter);
    }

    /// Uses the same rule the resolver does rather than a hardcoded square,
    /// so the fixture's own layout can change without silently turning one
    /// of these two tests into a duplicate of the other.
    private static EncounterCombatMovementDestinationOption FindDestination(
        Fixture fixture,
        bool provoking)
    {
        EncounterCombatMovementOption movement =
            Assert.IsType<EncounterCombatMovementOption>(
                fixture.Decision.Movement);
        GridPosition start = fixture.Encounter.Participants
            .Single(participant => string.Equals(
                participant.Combatant.CombatantId,
                fixture.ActorId,
                StringComparison.Ordinal))
            .Position;

        return movement.DestinationOptions.First(destination =>
            PathProvokes(fixture.Encounter, fixture.ActorId, start, destination)
                == provoking);
    }

    private static bool PathProvokes(
        EncounterState encounter,
        string actorId,
        GridPosition start,
        EncounterCombatMovementDestinationOption destination)
    {
        GridPosition previous = start;

        foreach (GridPosition next in destination.Path)
        {
            if (EncounterOpportunityAttackRules.FindProvocations(
                encounter,
                actorId,
                previous,
                next).Count > 0)
            {
                return true;
            }

            previous = next;
        }

        return false;
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
        Assert.NotNull(resolution.PrimaryStep);
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

        Assert.NotNull(resolution.PrimaryStep);
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
