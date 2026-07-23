using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatDecisionTests
{
    [Fact]
    public void AdvanceToDecision_WithConsciousPartyActor_ReturnsStructuredPlayerDecision()
    {
        ApplicationSessionState state =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(state);

        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.StartingDecision.State);
        Assert.Equal(
            WatchtowerCombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.NotNull(result.ResultingDecision.ActiveCombatantId);
        Assert.NotNull(result.ResultingDecision.Movement);
        Assert.NotNull(result.ResultingDecision.WeaponAttack);
        Assert.NotNull(result.ResultingDecision.EndTurn);
        Assert.True(result.ResultingDecision.EndTurn.IsAvailable);
        Assert.Empty(result.AutomaticSteps);
        Assert.Equal(
            result.RandomValuesConsumedBefore,
            result.RandomValuesConsumedAfter);
        Assert.Null(result.PrimaryStep);
        Assert.Null(result.SubmittedIntent);
    }

    [Fact]
    public void AdvanceToDecision_ExposesOrderedCoreValidatedMovementDestinations()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        WatchtowerCombatMovementOption movement =
            Assert.IsType<WatchtowerCombatMovementOption>(
                decision.Movement);
        IReadOnlyList<WatchtowerCombatMovementDestinationOption> options =
            movement.DestinationOptions;
        HashSet<GridPosition> blocked =
            encounter.Battlefield.BlockedPositions.ToHashSet();
        HashSet<GridPosition> occupied = encounter.Participants
            .Where(participant => !string.Equals(
                participant.Combatant.CombatantId,
                actor.Combatant.CombatantId,
                StringComparison.Ordinal))
            .Select(participant => participant.Position)
            .ToHashSet();

        Assert.True(movement.IsAvailable);
        Assert.NotEmpty(options);
        Assert.Equal(
            options.Count,
            options.Select(option => option.Destination).Distinct().Count());
        Assert.Equal(
            options.Count,
            options.Select(option => string.Join(
                ";",
                option.Path.Select(position =>
                    $"{position.X},{position.Y}")))
                .Distinct(StringComparer.Ordinal)
                .Count());

        for (int index = 0; index < options.Count; index++)
        {
            WatchtowerCombatMovementDestinationOption option =
                options[index];

            Assert.NotEmpty(option.Path);
            Assert.DoesNotContain(actor.Position, option.Path);
            Assert.Equal(option.Destination, option.Path[^1]);
            Assert.True(option.MovementSpentFeet > 0);
            Assert.True(
                option.MovementSpentFeet
                    <= movement.MovementRemainingFeet);

            GridPosition previous = actor.Position;

            foreach (GridPosition position in option.Path)
            {
                Assert.InRange(
                    position.X,
                    0,
                    encounter.Battlefield.Width - 1);
                Assert.InRange(
                    position.Y,
                    0,
                    encounter.Battlefield.Height - 1);
                Assert.DoesNotContain(position, blocked);
                Assert.DoesNotContain(position, occupied);
                Assert.InRange(
                    Math.Abs(position.X - previous.X),
                    0,
                    1);
                Assert.InRange(
                    Math.Abs(position.Y - previous.Y),
                    0,
                    1);
                Assert.NotEqual(previous, position);
                previous = position;
            }

            EncounterMovementResult resolved =
                EncounterMovementRules.Resolve(
                    encounter,
                    new EncounterMovementCommand
                    {
                        ExpectedRevision = decision.EncounterRevision,
                        ActorCombatantId = decision.ActiveCombatantId!,
                        Path = option.Path
                    });

            Assert.Equal(option.Destination, resolved.EndingPosition);
            Assert.Equal(
                option.MovementSpentFeet,
                resolved.MovementSpentFeet);

            if (index > 0)
            {
                Assert.True(
                    CompareMovementOptions(
                        options[index - 1],
                        option) <= 0);
            }
        }

        Assert.NotNull(decision.WeaponAttack);
        Assert.NotNull(decision.EndTurn);
        Assert.True(decision.EndTurn.IsAvailable);
    }

    [Fact]
    public void AdvanceToDecision_MovementCollectionsAreReadOnlyIndependentAndDeterministic()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatMovementOption first =
            Assert.IsType<WatchtowerCombatMovementOption>(
                WatchtowerCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);
        WatchtowerCombatMovementOption second =
            Assert.IsType<WatchtowerCombatMovementOption>(
                WatchtowerCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);
        IList<WatchtowerCombatMovementDestinationOption> mutableOptions =
            Assert.IsAssignableFrom<
                IList<WatchtowerCombatMovementDestinationOption>>(
                    first.DestinationOptions);

        Assert.True(mutableOptions.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            mutableOptions.Add(first.DestinationOptions[0]));
        Assert.NotSame(
            first.DestinationOptions,
            second.DestinationOptions);
        Assert.Equal(
            first.DestinationOptions.Count,
            second.DestinationOptions.Count);

        for (int index = 0;
            index < first.DestinationOptions.Count;
            index++)
        {
            WatchtowerCombatMovementDestinationOption firstOption =
                first.DestinationOptions[index];
            WatchtowerCombatMovementDestinationOption secondOption =
                second.DestinationOptions[index];
            IList<GridPosition> mutablePath =
                Assert.IsAssignableFrom<IList<GridPosition>>(
                    firstOption.Path);

            Assert.True(mutablePath.IsReadOnly);
            Assert.Throws<NotSupportedException>(() =>
                mutablePath.Add(firstOption.Destination));
            Assert.NotSame(firstOption.Path, secondOption.Path);
            Assert.Equal(
                firstOption.Destination,
                secondOption.Destination);
            Assert.Equal(
                firstOption.MovementSpentFeet,
                secondOption.MovementSpentFeet);
            Assert.Equal(
                firstOption.Path.ToArray(),
                secondOption.Path.ToArray());
        }

        GridPosition[] copiedPath =
            first.DestinationOptions[0].Path.ToArray();
        copiedPath[0] = new GridPosition(-1, -1);

        Assert.NotEqual(
            copiedPath[0],
            second.DestinationOptions[0].Path[0]);
    }

    [Fact]
    public void AdvanceToDecision_WithNoRemainingMovement_ReturnsNoDestinations()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            actor with
            {
                TurnResources = actor.TurnResources with
                {
                    MovementSpentFeet =
                        actor.TurnResources.MovementSpeedFeet
                }
            });

        WatchtowerCombatMovementOption movement =
            Assert.IsType<WatchtowerCombatMovementOption>(
                WatchtowerCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);

        Assert.False(movement.IsAvailable);
        Assert.Equal(0, movement.MovementRemainingFeet);
        Assert.Equal(
            EncounterActionUnavailabilityReason.MovementUnavailable,
            movement.UnavailabilityReason);
        Assert.Empty(movement.DestinationOptions);
    }

    [Fact]
    public void AdvanceToDecision_WithNoReachableSquare_ReturnsNoDestinations()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        HashSet<GridPosition> occupied = encounter.Participants
            .Where(participant => !string.Equals(
                participant.Combatant.CombatantId,
                actor.Combatant.CombatantId,
                StringComparison.Ordinal))
            .Select(participant => participant.Position)
            .ToHashSet();
        List<GridPosition> blocked = [];

        for (int y = actor.Position.Y - 1;
            y <= actor.Position.Y + 1;
            y++)
        {
            for (int x = actor.Position.X - 1;
                x <= actor.Position.X + 1;
                x++)
            {
                GridPosition position = new(x, y);

                if (position == actor.Position
                    || position.X < 0
                    || position.X >= encounter.Battlefield.Width
                    || position.Y < 0
                    || position.Y >= encounter.Battlefield.Height
                    || occupied.Contains(position))
                {
                    continue;
                }

                blocked.Add(position);
            }
        }

        source = WatchtowerCombatTestData.ReplaceEncounter(
            source,
            encounter with
            {
                Battlefield = encounter.Battlefield with
                {
                    BlockedPositions = Array.AsReadOnly(
                        blocked.ToArray())
                }
            });

        WatchtowerCombatMovementOption movement =
            Assert.IsType<WatchtowerCombatMovementOption>(
                WatchtowerCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);

        Assert.False(movement.IsAvailable);
        Assert.Equal(
            EncounterActionUnavailabilityReason.MovementUnavailable,
            movement.UnavailabilityReason);
        Assert.Empty(movement.DestinationOptions);
    }

    [Fact]
    public void AdvanceToDecision_ExposesFixedWeaponAndAuthoritativeTargets()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerCombatTestData.CreatePlayerDecisionSession(),
                "party-member.ranger");
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger") with
            {
                Position = new GridPosition(2, 2)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            ranger);
        WeaponAttack expectedWeapon = Assert.Single(
            ranger.CombatProfile.WeaponAttacks);

        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        WatchtowerCombatWeaponAttackOption attack =
            Assert.IsType<WatchtowerCombatWeaponAttackOption>(
                decision.WeaponAttack);

        Assert.Equal(expectedWeapon.WeaponId, attack.WeaponId);
        Assert.Equal(2, attack.Targets.Count);

        WatchtowerCombatTargetOption meleeTarget =
            Assert.Single(
                attack.Targets,
                target => string.Equals(
                    target.TargetCombatantId,
                    WatchtowerSignalEncounter.MeleeRaiderId,
                    StringComparison.Ordinal));

        Assert.True(meleeTarget.IsAvailable);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            meleeTarget.UnavailabilityReason);
        Assert.Equal(
            D20RollMode.Disadvantage,
            meleeTarget.AttackRollMode);
        Assert.Equal(5, meleeTarget.DistanceFeet);

        WatchtowerCombatTargetOption rangedTarget =
            Assert.Single(
                attack.Targets,
                target => string.Equals(
                    target.TargetCombatantId,
                    WatchtowerSignalEncounter.RangedRaiderId,
                    StringComparison.Ordinal));

        Assert.True(rangedTarget.IsAvailable);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            rangedTarget.UnavailabilityReason);
        Assert.Equal(
            D20RollMode.Disadvantage,
            rangedTarget.AttackRollMode);
        Assert.Equal(10, rangedTarget.DistanceFeet);
    }

    [Fact]
    public void AdvanceToDecision_WhenRaiderStarts_ProcessesAutomatically()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            WatchtowerCombatDecisionState.AutomaticProcessingRequired,
            result.StartingDecision.State);
        Assert.NotEmpty(result.AutomaticSteps);
        Assert.NotEqual(
            WatchtowerCombatDecisionState.AutomaticProcessingRequired,
            result.ResultingDecision.State);
    }

    [Fact]
    public void AdvanceToDecision_WhenCompleted_IsIdempotent()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active = Assert.IsType<ActiveEncounterState>(
            source.ActiveEncounter);
        EncounterState completed = EncounterRules.Complete(
            active.Encounter,
            "side.party");
        source = WatchtowerCombatTestData.ReplaceEncounter(
            source,
            completed);

        WatchtowerCombatResolutionResult result =
            WatchtowerCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.StartingDecision.State);
        Assert.Equal(
            WatchtowerCombatDecisionState.CombatCompleted,
            result.ResultingDecision.State);
        Assert.Equal("side.party", result.ResultingDecision.WinningSideId);
        Assert.Null(result.ResultingDecision.Movement);
        Assert.Empty(result.AutomaticSteps);
        Assert.Equal(source.RandomValuesConsumed, result.RandomValuesConsumedAfter);
    }

    [Fact]
    public void Decision_WhenRangerHasNoArrows_ReportsStructuredUnavailability()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        string rangerId = source.Party.Members[2].PartyMemberId;
        source = WatchtowerCombatTestData.AdvanceToCombatant(
            source,
            rangerId);
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(source, rangerId);
        WeaponAttack weapon = Assert.Single(ranger.CombatProfile.WeaponAttacks);
        ranger = ranger with
        {
            CombatProfile = ranger.CombatProfile with
            {
                WeaponAttacks =
                [
                    weapon with
                    {
                        AmmunitionQuantityAvailable = 0
                    }
                ]
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, ranger);

        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(source)
                .ResultingDecision;

        Assert.False(decision.WeaponAttack!.IsAvailable);
        Assert.All(
            decision.WeaponAttack.Targets,
            target => Assert.Equal(
                EncounterActionUnavailabilityReason.AmmunitionUnavailable,
                target.UnavailabilityReason));
        Assert.True(decision.Movement!.IsAvailable);
        Assert.True(decision.EndTurn!.IsAvailable);
    }

    [Theory]
    [InlineData(CompletedIntentKind.Move)]
    [InlineData(CompletedIntentKind.WeaponAttack)]
    [InlineData(CompletedIntentKind.EndTurn)]
    public void Execute_WhenCombatCompleted_RejectsEveryPlayerIntentWithoutStateOrDice(
        CompletedIntentKind intentKind)
    {
        ApplicationSessionState activeSource =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        WatchtowerCombatDecision activeDecision =
            WatchtowerCombatRules.AdvanceToDecision(activeSource)
                .ResultingDecision;
        WatchtowerCombatTargetOption target =
            activeDecision.WeaponAttack!.Targets.First(
                candidate => candidate.IsAvailable);
        EncounterState activeEncounter =
            WatchtowerCombatTestData.GetEncounter(activeSource);
        ApplicationSessionState source =
            WatchtowerCombatTestData.ReplaceEncounter(
                activeSource,
                EncounterRules.Complete(activeEncounter, "side.party"));
        EncounterState completedBefore =
            WatchtowerCombatTestData.GetEncounter(source);
        int cursorBefore = source.RandomValuesConsumed;
        PartyMemberStateSnapshot[] partyBefore = source.Party.Members
            .Select(member => new PartyMemberStateSnapshot(
                member.PartyMemberId,
                member.Health,
                member.Ammunition))
            .ToArray();
        ExplorationState returnContextBefore =
            source.ActiveEncounter!.ReturnContext;
        CompletedParticipantSnapshot[] participantsBefore =
            completedBefore.Participants
                .Select(participant => new CompletedParticipantSnapshot(
                    participant.Combatant.CombatantId,
                    participant.Position,
                    participant.TurnResources,
                    participant.Combatant.Health,
                    participant.CombatProfile.WeaponAttacks
                        .Select(weapon => new CompletedWeaponSnapshot(
                            weapon.WeaponId,
                            weapon.AmmunitionQuantityAvailable))
                        .ToArray()))
                .ToArray();

        Action operation = intentKind switch
        {
            CompletedIntentKind.Move => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatMoveIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = completedBefore.ActiveCombatantId,
                        Path = [new GridPosition(2, 0)]
                    });
            },
            CompletedIntentKind.WeaponAttack => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = activeDecision.ActiveCombatantId!,
                        WeaponId = activeDecision.WeaponAttack.WeaponId,
                        TargetCombatantId = target.TargetCombatantId
                    });
            },
            CompletedIntentKind.EndTurn => () =>
            {
                _ = WatchtowerCombatRules.Execute(
                    source,
                    new WatchtowerCombatEndTurnIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = completedBefore.ActiveCombatantId
                    });
            },
            _ => throw new InvalidOperationException(
                "Unsupported completed-intent test case.")
        };

        Assert.Throws<InvalidOperationException>(operation);

        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            source.Scenario.Progress);
        Assert.Equal(returnContextBefore, source.ActiveEncounter!.ReturnContext);
        Assert.Equal(cursorBefore, source.RandomValuesConsumed);
        Assert.Equal(partyBefore.Length, source.Party.Members.Count);

        for (int index = 0; index < partyBefore.Length; index++)
        {
            Assert.Equal(
                partyBefore[index].PartyMemberId,
                source.Party.Members[index].PartyMemberId);
            Assert.Equal(
                partyBefore[index].Health,
                source.Party.Members[index].Health);
            Assert.Equal(
                partyBefore[index].Ammunition,
                source.Party.Members[index].Ammunition);
        }

        EncounterState completedAfter =
            WatchtowerCombatTestData.GetEncounter(source);
        Assert.Equal(completedBefore.Revision, completedAfter.Revision);
        Assert.Equal(completedBefore.ActiveCombatantId, completedAfter.ActiveCombatantId);
        Assert.Equal(EncounterLifecycleState.Completed, completedAfter.LifecycleState);
        Assert.Equal("side.party", completedAfter.WinningSideId);
        Assert.Equal(participantsBefore.Length, completedAfter.Participants.Count);

        for (int index = 0; index < participantsBefore.Length; index++)
        {
            CompletedParticipantSnapshot expected = participantsBefore[index];
            EncounterParticipantState actual = completedAfter.Participants[index];
            Assert.Equal(expected.CombatantId, actual.Combatant.CombatantId);
            Assert.Equal(expected.Position, actual.Position);
            Assert.Equal(expected.TurnResources, actual.TurnResources);
            Assert.Equal(expected.Health, actual.Combatant.Health);
            Assert.Equal(expected.Weapons.Length, actual.CombatProfile.WeaponAttacks.Count);

            for (int weaponIndex = 0;
                weaponIndex < expected.Weapons.Length;
                weaponIndex++)
            {
                Assert.Equal(
                    expected.Weapons[weaponIndex].WeaponId,
                    actual.CombatProfile.WeaponAttacks[weaponIndex].WeaponId);
                Assert.Equal(
                    expected.Weapons[weaponIndex].AmmunitionQuantityAvailable,
                    actual.CombatProfile.WeaponAttacks[weaponIndex]
                        .AmmunitionQuantityAvailable);
            }
        }
    }


    private static int CompareMovementOptions(
        WatchtowerCombatMovementDestinationOption left,
        WatchtowerCombatMovementDestinationOption right)
    {
        int comparison = left.Destination.Y.CompareTo(
            right.Destination.Y);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Destination.X.CompareTo(
            right.Destination.X);

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.MovementSpentFeet.CompareTo(
            right.MovementSpentFeet);

        if (comparison != 0)
        {
            return comparison;
        }

        int sharedCount = Math.Min(left.Path.Count, right.Path.Count);

        for (int index = 0; index < sharedCount; index++)
        {
            comparison = left.Path[index].Y.CompareTo(
                right.Path[index].Y);

            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Path[index].X.CompareTo(
                right.Path[index].X);

            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.Path.Count.CompareTo(right.Path.Count);
    }

    public enum CompletedIntentKind
    {
        Move,
        WeaponAttack,
        EndTurn
    }

    private sealed record PartyMemberStateSnapshot(
        string PartyMemberId,
        FiveEGoldBox.Core.Rules.CombatantHealthState Health,
        AmmunitionState? Ammunition);

    private sealed record CompletedParticipantSnapshot(
        string CombatantId,
        GridPosition Position,
        CombatTurnResources TurnResources,
        FiveEGoldBox.Core.Rules.CombatantHealthState Health,
        CompletedWeaponSnapshot[] Weapons);

    private sealed record CompletedWeaponSnapshot(
        string WeaponId,
        int? AmmunitionQuantityAvailable);
}
