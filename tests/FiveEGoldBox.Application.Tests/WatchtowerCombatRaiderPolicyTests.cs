using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatRaiderPolicyTests
{
    [Fact]
    public void SelectTarget_EqualDistanceUsesPersistentPartyOrder()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");

        EncounterParticipantState? target =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                source.Party,
                raider);

        Assert.NotNull(target);
        Assert.Equal(
            "party-member.fighter",
            target.Combatant.CombatantId);
    }

    [Theory]
    [InlineData(NonConsciousTargetState.Dying)]
    [InlineData(NonConsciousTargetState.Stable)]
    [InlineData(NonConsciousTargetState.Dead)]
    [InlineData(NonConsciousTargetState.Defeated)]
    public void SelectTarget_ExcludesPreferredNonconsciousParticipant(
        NonConsciousTargetState targetState)
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterParticipantState fighter =
            ApplyNonConsciousState(
                WatchtowerCombatTestData.GetParticipant(
                    source,
                    "party-member.fighter"),
                targetState);
        source = WatchtowerCombatTestData.ReplaceParticipant(source, fighter);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        int cursorBefore = source.RandomValuesConsumed;

        EncounterParticipantState? target =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                source.Party,
                raider);

        Assert.NotNull(target);
        Assert.Equal(
            "party-member.barbarian",
            target.Combatant.CombatantId);
        Assert.NotEqual(
            CombatantLifecycleState.Conscious,
            fighter.Combatant.LifecycleState);
        Assert.Equal(cursorBefore, source.RandomValuesConsumed);
    }

    [Fact]
    public void SelectTarget_ChangedDisplayNamesDoNotChangeSelection()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        EncounterParticipantState expected =
            Assert.IsType<EncounterParticipantState>(
                WatchtowerRaiderPolicy.SelectTarget(
                    encounter,
                    source.Party,
                    raider));
        PartyMemberState[] renamedMembers = source.Party.Members
            .Select((member, index) => member with
            {
                DisplayName = index switch
                {
                    0 => "Zulu",
                    1 => "Alpha",
                    _ => "Mike"
                }
            })
            .ToArray();
        PartyState renamedParty = source.Party with
        {
            Members = Array.AsReadOnly(renamedMembers)
        };
        int cursorBefore = source.RandomValuesConsumed;

        EncounterParticipantState actual =
            Assert.IsType<EncounterParticipantState>(
                WatchtowerRaiderPolicy.SelectTarget(
                    encounter,
                    renamedParty,
                    raider));

        Assert.Equal(
            expected.Combatant.CombatantId,
            actual.Combatant.CombatantId);
        Assert.Equal(cursorBefore, source.RandomValuesConsumed);
    }

    [Fact]
    public void SelectTarget_ReorderedParticipantCollectionKeepsTargetAndPath()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee") with
            {
                Position = new GridPosition(4, 3)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState expectedTarget =
            Assert.IsType<EncounterParticipantState>(
                WatchtowerRaiderPolicy.SelectTarget(
                    encounter,
                    source.Party,
                    raider));
        string weaponId = Assert.Single(
            raider.CombatProfile.WeaponAttacks).WeaponId;
        EncounterMovementResult expectedMovement =
            Assert.IsType<EncounterMovementResult>(
                WatchtowerCombatPathSearch.FindMovement(
                    encounter,
                    raider.Combatant.CombatantId,
                    expectedTarget.Combatant.CombatantId,
                    weaponId));
        int cursorBefore = source.RandomValuesConsumed;

        EncounterState reorderedEncounter = encounter with
        {
            Participants = Array.AsReadOnly(
                encounter.Participants.Reverse().ToArray())
        };
        ApplicationSessionState reorderedSource =
            WatchtowerCombatTestData.ReplaceEncounter(
                source,
                reorderedEncounter);
        EncounterParticipantState reorderedRaider =
            WatchtowerCombatTestData.GetParticipant(
                reorderedSource,
                "combatant.watchtower-raider.melee");
        EncounterParticipantState actualTarget =
            Assert.IsType<EncounterParticipantState>(
                WatchtowerRaiderPolicy.SelectTarget(
                    reorderedEncounter,
                    reorderedSource.Party,
                    reorderedRaider));
        EncounterMovementResult actualMovement =
            Assert.IsType<EncounterMovementResult>(
                WatchtowerCombatPathSearch.FindMovement(
                    reorderedEncounter,
                    reorderedRaider.Combatant.CombatantId,
                    actualTarget.Combatant.CombatantId,
                    weaponId));

        Assert.Equal(
            expectedTarget.Combatant.CombatantId,
            actualTarget.Combatant.CombatantId);
        Assert.Equal(
            expectedMovement.Path.ToArray(),
            actualMovement.Path.ToArray());
        Assert.Equal(
            expectedMovement.EndingPosition,
            actualMovement.EndingPosition);
        Assert.True(
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                expectedMovement.State,
                raider.Combatant.CombatantId,
                expectedTarget.Combatant.CombatantId,
                weaponId).IsLegal);
        Assert.True(
            EncounterWeaponAttackPrerequisiteRules.Evaluate(
                actualMovement.State,
                reorderedRaider.Combatant.CombatantId,
                actualTarget.Combatant.CombatantId,
                weaponId).IsLegal);
        Assert.Equal(cursorBefore, source.RandomValuesConsumed);
        Assert.Equal(cursorBefore, reorderedSource.RandomValuesConsumed);
    }

    [Fact]
    public void SelectTarget_WhenPartyOrderCannotResolveTie_UsesOrdinalCombatantId()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        PartyMemberState[] unrelatedMembers = source.Party.Members
            .Select((member, index) => member with
            {
                PartyMemberId = $"unrelated.{index}"
            })
            .ToArray();
        PartyState unrelatedParty = source.Party with
        {
            Members = Array.AsReadOnly(unrelatedMembers)
        };
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");

        EncounterParticipantState? target =
            WatchtowerRaiderPolicy.SelectTarget(
                encounter,
                unrelatedParty,
                raider);

        Assert.NotNull(target);
        Assert.Equal(
            "party-member.barbarian",
            target.Combatant.CombatantId);
    }

    [Fact]
    public void FindMovement_SymmetricChoicesUseBoundedDestinationAndPathOrdering()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee") with
            {
                Position = new GridPosition(4, 3)
            };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        EncounterMovementResult? movement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");

        Assert.NotNull(movement);
        Assert.Equal(
            new[]
            {
                new GridPosition(3, 2),
                new GridPosition(2, 1)
            },
            movement.Path);
        Assert.Equal(new GridPosition(2, 1), movement.EndingPosition);
    }

    [Fact]
    public void FindMovement_EqualLengthPathUsesLowestYThenLowestXNeighborOrder()
    {
        ApplicationSessionState source = CreateProgressSearchSession(
            actorPosition: new GridPosition(4, 2),
            blockedPositions:
            [
                new GridPosition(2, 0)
            ]);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        EncounterMovementResult? movement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");

        Assert.NotNull(movement);
        Assert.Equal(new GridPosition(2, 1), movement.EndingPosition);
        Assert.Equal(
            new[]
            {
                new GridPosition(3, 1),
                new GridPosition(2, 1)
            },
            movement.Path);
    }

    [Fact]
    public void FindMovement_EqualProgressDestinationsUseCoordinatesBeforeMovementSpent()
    {
        ApplicationSessionState source = CreateProgressSearchSession(
            actorPosition: new GridPosition(3, 2),
            blockedPositions:
            [
                new GridPosition(1, 0),
                new GridPosition(1, 1)
            ]);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        EncounterMovementResult? movement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");

        Assert.NotNull(movement);
        Assert.Equal(10, movement.MovementSpentFeet);
        Assert.Equal(new GridPosition(2, 0), movement.EndingPosition);
        Assert.Equal(
            new[]
            {
                new GridPosition(2, 1),
                new GridPosition(2, 0)
            },
            movement.Path);
    }

    [Fact]
    public void FindMovement_UnexpectedCoreInvariantFailurePropagates()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.ranged");
        EncounterParticipantState meleeRaider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        meleeRaider = meleeRaider with
        {
            TurnResources = meleeRaider.TurnResources with
            {
                MovementSpentFeet = 0
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            meleeRaider);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        Assert.Throws<InvalidOperationException>(() =>
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar"));
    }

    [Fact]
    public void FindMovement_WithNoMovementRemaining_ReturnsNull()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        raider = raider with
        {
            TurnResources = raider.TurnResources with
            {
                MovementSpentFeet =
                    raider.TurnResources.MovementSpeedFeet
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);
        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);

        EncounterMovementResult? movement =
            WatchtowerCombatPathSearch.FindMovement(
                encounter,
                "combatant.watchtower-raider.melee",
                "party-member.fighter",
                "weapon.watchtower-raider.scimitar");

        Assert.Null(movement);
    }

    private static EncounterParticipantState ApplyNonConsciousState(
        EncounterParticipantState participant,
        NonConsciousTargetState targetState)
    {
        int maximumHitPoints =
            participant.Combatant.Health.HitPoints.MaximumHitPoints;

        return targetState switch
        {
            NonConsciousTargetState.Dying => participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        maximumHitPoints,
                        successes: 0,
                        failures: 0,
                        isStable: false)
                }
            },
            NonConsciousTargetState.Stable => participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        maximumHitPoints,
                        successes: 0,
                        failures: 0,
                        isStable: true)
                }
            },
            NonConsciousTargetState.Dead => participant with
            {
                Combatant = participant.Combatant with
                {
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        maximumHitPoints,
                        successes: 0,
                        failures: 3,
                        isStable: false)
                }
            },
            NonConsciousTargetState.Defeated => participant with
            {
                Combatant = participant.Combatant with
                {
                    ZeroHitPointPolicy = CombatantZeroHitPointPolicy.Defeated,
                    Health = WatchtowerCombatTestData.CreateZeroHealth(
                        maximumHitPoints,
                        successes: 0,
                        failures: 0,
                        isStable: false)
                }
            },
            _ => throw new InvalidOperationException(
                "Unsupported non-Conscious lifecycle test case.")
        };
    }

    public enum NonConsciousTargetState
    {
        Dying,
        Stable,
        Dead,
        Defeated
    }

    private static ApplicationSessionState CreateProgressSearchSession(
        GridPosition actorPosition,
        IReadOnlyList<GridPosition> blockedPositions)
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerSignalTestData.CreateEncounterSession(),
                "combatant.watchtower-raider.melee");

        source = SetPosition(
            source,
            "party-member.fighter",
            new GridPosition(0, 0));
        source = SetPosition(
            source,
            "party-member.barbarian",
            new GridPosition(0, 3));
        source = SetPosition(
            source,
            "party-member.ranger",
            new GridPosition(1, 3));
        source = SetPosition(
            source,
            "combatant.watchtower-raider.ranged",
            new GridPosition(4, 3));

        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee") with
            {
                Position = actorPosition
            };
        raider = raider with
        {
            TurnResources = raider.TurnResources with
            {
                MovementSpentFeet = 20
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(source, raider);

        EncounterState encounter = WatchtowerCombatTestData.GetEncounter(source);
        encounter = encounter with
        {
            Battlefield = encounter.Battlefield with
            {
                BlockedPositions =
                    Array.AsReadOnly(blockedPositions.ToArray())
            }
        };

        return WatchtowerCombatTestData.ReplaceEncounter(source, encounter);
    }

    private static ApplicationSessionState SetPosition(
        ApplicationSessionState source,
        string combatantId,
        GridPosition position)
    {
        EncounterParticipantState participant =
            WatchtowerCombatTestData.GetParticipant(
                source,
                combatantId) with
            {
                Position = position
            };

        return WatchtowerCombatTestData.ReplaceParticipant(
            source,
            participant);
    }
}
