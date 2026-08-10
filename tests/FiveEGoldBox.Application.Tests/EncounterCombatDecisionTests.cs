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

public sealed class EncounterCombatDecisionTests
{
    [Fact]
    public void AdvanceToDecision_WithConsciousPartyActor_ReturnsStructuredPlayerDecision()
    {
        ApplicationSessionState state =
            EncounterCombatTestData.CreatePlayerDecisionSession();

        EncounterCombatResolutionResult result =
            EncounterCombatRules.AdvanceToDecision(state);

        Assert.Equal(
            CombatDecisionState.PlayerDecisionRequired,
            result.StartingDecision.State);
        Assert.Equal(
            CombatDecisionState.PlayerDecisionRequired,
            result.ResultingDecision.State);
        Assert.NotNull(result.ResultingDecision.ActiveCombatantId);
        Assert.NotNull(result.ResultingDecision.Movement);
        Assert.NotEmpty(result.ResultingDecision.WeaponAttacks);
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            EncounterCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        EncounterCombatDecision decision =
            EncounterCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        EncounterCombatMovementOption movement =
            Assert.IsType<EncounterCombatMovementOption>(
                decision.Movement);
        IReadOnlyList<EncounterCombatMovementDestinationOption> options =
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
            EncounterCombatMovementDestinationOption option =
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

        Assert.NotEmpty(decision.WeaponAttacks);
        Assert.NotNull(decision.EndTurn);
        Assert.True(decision.EndTurn.IsAvailable);
    }

    [Fact]
    public void AdvanceToDecision_MovementCollectionsAreReadOnlyIndependentAndDeterministic()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterCombatMovementOption first =
            Assert.IsType<EncounterCombatMovementOption>(
                EncounterCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);
        EncounterCombatMovementOption second =
            Assert.IsType<EncounterCombatMovementOption>(
                EncounterCombatRules.AdvanceToDecision(source)
                    .ResultingDecision.Movement);
        IList<EncounterCombatMovementDestinationOption> mutableOptions =
            Assert.IsAssignableFrom<
                IList<EncounterCombatMovementDestinationOption>>(
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
            EncounterCombatMovementDestinationOption firstOption =
                first.DestinationOptions[index];
            EncounterCombatMovementDestinationOption secondOption =
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            EncounterCombatTestData.GetParticipant(
                source,
                encounter.ActiveCombatantId);
        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            actor with
            {
                TurnResources = actor.TurnResources with
                {
                    MovementSpentFeet =
                        actor.TurnResources.MovementSpeedFeet
                }
            });

        EncounterCombatMovementOption movement =
            Assert.IsType<EncounterCombatMovementOption>(
                EncounterCombatRules.AdvanceToDecision(source)
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            EncounterCombatTestData.GetParticipant(
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

        source = EncounterCombatTestData.ReplaceEncounter(
            source,
            encounter with
            {
                Battlefield = encounter.Battlefield with
                {
                    BlockedPositions = Array.AsReadOnly(
                        blocked.ToArray())
                }
            });

        EncounterCombatMovementOption movement =
            Assert.IsType<EncounterCombatMovementOption>(
                EncounterCombatRules.AdvanceToDecision(source)
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
            EncounterCombatTestData.AdvanceToCombatant(
                EncounterCombatTestData.CreatePlayerDecisionSession(),
                "party-member.rogue");
        EncounterParticipantState archer =
            EncounterCombatTestData.GetParticipant(
                source,
                "party-member.rogue") with
            {
                Position = new GridPosition(2, 2)
            };
        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            archer);
        WeaponAttack expectedWeapon = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        EncounterCombatDecision decision =
            EncounterCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        EncounterCombatWeaponAttackOption attack =
            Assert.Single(
                decision.WeaponAttacks,
                candidate => candidate.WeaponId
                    == expectedWeapon.WeaponId);

        Assert.Equal(expectedWeapon.WeaponId, attack.WeaponId);
        Assert.Equal(2, attack.Targets.Count);

        EncounterCombatTargetOption meleeTarget =
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

        EncounterCombatTargetOption rangedTarget =
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
            EncounterCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        EncounterCombatResolutionResult result =
            EncounterCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            CombatDecisionState.AutomaticProcessingRequired,
            result.StartingDecision.State);
        Assert.NotEmpty(result.AutomaticSteps);
        Assert.NotEqual(
            CombatDecisionState.AutomaticProcessingRequired,
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
        source = EncounterCombatTestData.ReplaceEncounter(
            source,
            completed);

        EncounterCombatResolutionResult result =
            EncounterCombatRules.AdvanceToDecision(source);

        Assert.Equal(
            CombatDecisionState.CombatCompleted,
            result.StartingDecision.State);
        Assert.Equal(
            CombatDecisionState.CombatCompleted,
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        string archerId = source.Party
            .Members[CampaignTestParty.ArcherIndex()]
            .PartyMemberId;
        source = EncounterCombatTestData.AdvanceToCombatant(
            source,
            archerId);
        EncounterParticipantState archer =
            EncounterCombatTestData.GetParticipant(source, archerId);
        WeaponAttack bow = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        // Empties only the bow. The archer's other weapon — a dagger, with no
        // ammunition to run out of — must stay unaffected by this.
        archer = archer with
        {
            CombatProfile = archer.CombatProfile with
            {
                WeaponAttacks = archer.CombatProfile.WeaponAttacks
                    .Select(candidate => string.Equals(
                        candidate.WeaponId,
                        bow.WeaponId,
                        StringComparison.Ordinal)
                        ? candidate with
                        {
                            AmmunitionQuantityAvailable = 0
                        }
                        : candidate)
                    .ToArray()
            }
        };
        source = EncounterCombatTestData.ReplaceParticipant(source, archer);

        EncounterCombatDecision decision =
            EncounterCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        EncounterCombatWeaponAttackOption bowOption = Assert.Single(
            decision.WeaponAttacks,
            candidate => candidate.WeaponId == bow.WeaponId);

        Assert.False(bowOption.IsAvailable);
        Assert.All(
            bowOption.Targets,
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterCombatDecision activeDecision =
            EncounterCombatRules.AdvanceToDecision(activeSource)
                .ResultingDecision;
        EncounterCombatTargetOption target =
            activeDecision.WeaponAttacks.Single().Targets.First(
                candidate => candidate.IsAvailable);
        EncounterState activeEncounter =
            EncounterCombatTestData.GetEncounter(activeSource);
        ApplicationSessionState source =
            EncounterCombatTestData.ReplaceEncounter(
                activeSource,
                EncounterRules.Complete(activeEncounter, "side.party"));
        EncounterState completedBefore =
            EncounterCombatTestData.GetEncounter(source);
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
                _ = EncounterCombatRules.Execute(
                    source,
                    new CombatMoveIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = completedBefore.ActiveCombatantId,
                        Path = [new GridPosition(2, 0)]
                    });
            },
            CompletedIntentKind.WeaponAttack => () =>
            {
                _ = EncounterCombatRules.Execute(
                    source,
                    new CombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision = completedBefore.Revision,
                        ActorCombatantId = activeDecision.ActiveCombatantId!,
                        WeaponId = activeDecision.WeaponAttacks.Single().WeaponId,
                        TargetCombatantId = target.TargetCombatantId
                    });
            },
            CompletedIntentKind.EndTurn => () =>
            {
                _ = EncounterCombatRules.Execute(
                    source,
                    new CombatEndTurnIntent
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
            WatchtowerScenario.ProgressOf(source));
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
            EncounterCombatTestData.GetEncounter(source);
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
        EncounterCombatMovementDestinationOption left,
        EncounterCombatMovementDestinationOption right)
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
