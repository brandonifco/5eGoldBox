using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class SignalMechanismRulesTests
{
    [Fact]
    public void CanActivate_AtAuthoredSignalState_ReturnsTrue()
    {
        Assert.True(
            SignalMechanismRules.CanActivate(
                WatchtowerSignalTestData.CreateSignalReadySession()));
    }

    [Fact]
    public void CanActivate_OnGroundFloor_ReturnsFalse()
    {
        Assert.False(
            SignalMechanismRules.CanActivate(
                WatchtowerSignalTestData.CreateExplorationSession()));
    }

    [Fact]
    public void CanActivate_AtWrongPosition_ReturnsFalse()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.MoveToUpperFloorStair();

        Assert.False(
            SignalMechanismRules.CanActivate(state));
    }

    [Fact]
    public void CanActivate_WithWrongFacing_ReturnsFalse()
    {
        ApplicationSessionState state =
            ExplorationRules.Turn(
                WatchtowerSignalTestData.CreateSignalReadySession(),
                ExplorationTurnDirection.Left);

        Assert.False(
            SignalMechanismRules.CanActivate(state));
    }

    [Theory]
    [InlineData(WatchtowerScenarioProgress.MissionNotAccepted)]
    [InlineData(WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(WatchtowerScenarioProgress.ScenarioCompleted)]
    public void CanActivate_WithWrongProgress_ReturnsFalse(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.False(
            SignalMechanismRules.CanActivate(state));
    }

    [Theory]
    [InlineData(ApplicationMode.Outpost)]
    [InlineData(ApplicationMode.RegionalTravel)]
    [InlineData(ApplicationMode.Encounter)]
    public void CanActivate_OutsideExploration_ReturnsFalse(
        ApplicationMode mode)
    {
        ApplicationSessionState state = mode switch
        {
            ApplicationMode.Outpost =>
                WatchtowerSignalTestData.CreateAcceptedSession(),
            ApplicationMode.RegionalTravel =>
                WatchtowerSignalTestData.CreateRegionalTravelSession(),
            ApplicationMode.Encounter =>
                WatchtowerSignalTestData.CreateEncounterSession(),
            _ => throw new InvalidOperationException(
                "The test mode is unsupported.")
        };

        Assert.False(
            SignalMechanismRules.CanActivate(state));
    }

    [Fact]
    public void CanActivate_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SignalMechanismRules.CanActivate(null!));
    }

    [Fact]
    public void CanActivate_WithMalformedExplorationSession_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession() with
            {
                CurrentLocationId = string.Empty
            };

        Assert.Throws<ArgumentException>(() =>
            SignalMechanismRules.CanActivate(state));
    }

    [Fact]
    public void Activate_AtSignalMechanism_EntersEncounterAndPreservesPersistentState()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateSignalReadySession();
        ValidatedRuleset ruleset = WatchtowerSignalTestData.CreateRuleset();

        ApplicationSessionState result =
            SignalMechanismRules.Activate(
                source,
                ruleset);

        Assert.Equal(
            ApplicationMode.Encounter,
            result.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            result.Scenario.Progress);
        Assert.Null(result.Exploration);
        Assert.Null(result.RegionalTravel);
        Assert.Equal(
            source.CurrentLocationId,
            result.CurrentLocationId);
        Assert.Equal(
            source.RandomSeed,
            result.RandomSeed);
        Assert.Equal(
            source.RandomValuesConsumed + 5,
            result.RandomValuesConsumed);
        AssertPartyEquivalent(
            source.Party,
            result.Party);

        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                result.ActiveEncounter);

        Assert.Equal(
            source.Exploration,
            active.ReturnContext);
    }

    [Fact]
    public void Activate_AtSignalMechanism_DoesNotMutateInputSession()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateSignalReadySession();
        ExplorationState originalExploration =
            Assert.IsType<ExplorationState>(
                source.Exploration);
        int originalRandomValuesConsumed =
            source.RandomValuesConsumed;

        _ = SignalMechanismRules.Activate(
            source,
            WatchtowerSignalTestData.CreateRuleset());

        Assert.Equal(
            ApplicationMode.Exploration,
            source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            source.Scenario.Progress);
        Assert.Equal(
            originalExploration,
            source.Exploration);
        Assert.Null(source.ActiveEncounter);
        Assert.Equal(
            originalRandomValuesConsumed,
            source.RandomValuesConsumed);
    }

    [Fact]
    public void Activate_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SignalMechanismRules.Activate(
                null!,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithNullRuleset_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData.CreateSignalReadySession(),
                null!));
    }

    [Fact]
    public void Activate_OutsideExploration_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData.CreateAcceptedSession(),
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_AtWrongPosition_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData.MoveToUpperFloorStair(),
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithWrongFacing_Throws()
    {
        ApplicationSessionState state =
            ExplorationRules.Turn(
                WatchtowerSignalTestData.CreateSignalReadySession(),
                ExplorationTurnDirection.Left);

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Theory]
    [InlineData(WatchtowerScenarioProgress.MissionNotAccepted)]
    [InlineData(WatchtowerScenarioProgress.SignalActivated)]
    [InlineData(WatchtowerScenarioProgress.RaidersDefeated)]
    [InlineData(WatchtowerScenarioProgress.SuccessReported)]
    [InlineData(WatchtowerScenarioProgress.ScenarioCompleted)]
    public void Activate_WithWrongScenarioProgress_Throws(
        WatchtowerScenarioProgress progress)
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress = progress
                }
            };

        Assert.Throws<ArgumentException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithPreexistingActiveEncounter_Throws()
    {
        ApplicationSessionState signalReady =
            WatchtowerSignalTestData.CreateSignalReadySession();
        ValidatedRuleset ruleset = WatchtowerSignalTestData.CreateRuleset();
        ApplicationSessionState activated =
            SignalMechanismRules.Activate(
                signalReady,
                ruleset);
        ApplicationSessionState invalid =
            signalReady with
            {
                ActiveEncounter =
                    activated.ActiveEncounter
            };

        Assert.Throws<ArgumentException>(() =>
            SignalMechanismRules.Activate(
                invalid,
                ruleset));
    }

    [Fact]
    public void Activate_WithUnresolvableCharacterDefinition_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();

        members[0] = members[0] with
        {
            CharacterDefinitionId =
                "character.unsupported"
        };

        state = state with
        {
            Party = state.Party with
            {
                Members = members
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithCharacterDefinitionAndClassMismatch_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();

        string fighterDefinitionId =
            members[0].CharacterDefinitionId;
        members[0] = members[0] with
        {
            CharacterDefinitionId =
                members[1].CharacterDefinitionId
        };
        members[1] = members[1] with
        {
            CharacterDefinitionId =
                fighterDefinitionId
        };

        state = state with
        {
            Party = state.Party with
            {
                Members = members
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithPersistentMaximumHitPointMismatch_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();
        CombatantHealthState health = members[0].Health;

        members[0] = members[0] with
        {
            Health = health with
            {
                HitPoints = health.HitPoints with
                {
                    MaximumHitPoints =
                        health.HitPoints.MaximumHitPoints + 1
                }
            }
        };

        state = state with
        {
            Party = state.Party with
            {
                Members = members
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithRangerAmmunitionWeaponMismatch_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();
        AmmunitionState ammunition =
            Assert.IsType<AmmunitionState>(
                members[2].Ammunition);

        members[2] = members[2] with
        {
            Ammunition = ammunition with
            {
                WeaponId = "weapon.other"
            }
        };

        state = state with
        {
            Party = state.Party with
            {
                Members = members
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WithRangerAmmunitionItemMismatch_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            state.Party.Members.ToArray();
        AmmunitionState ammunition =
            Assert.IsType<AmmunitionState>(
                members[2].Ammunition);

        members[2] = members[2] with
        {
            Ammunition = ammunition with
            {
                AmmunitionItemId = "item.other"
            }
        };

        state = state with
        {
            Party = state.Party with
            {
                Members = members
            }
        };

        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                state,
                WatchtowerSignalTestData.CreateRuleset()));
    }

    [Fact]
    public void Activate_WhenBoundedWeaponContentIsMissing_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData.CreateSignalReadySession(),
                WatchtowerSignalTestData.CreateRuleset(includeLongbow: false)));
    }

    private static void AssertPartyEquivalent(
        PartyState expected,
        PartyState actual)
    {
        Assert.Equal(expected.PartyId, actual.PartyId);
        Assert.Equal(
            expected.Members.Count,
            actual.Members.Count);

        for (int index = 0;
            index < expected.Members.Count;
            index++)
        {
            PartyMemberState expectedMember =
                expected.Members[index];
            PartyMemberState actualMember =
                actual.Members[index];

            Assert.Equal(
                expectedMember.PartyMemberId,
                actualMember.PartyMemberId);
            Assert.Equal(
                expectedMember.CharacterDefinitionId,
                actualMember.CharacterDefinitionId);
            Assert.Equal(
                expectedMember.DisplayName,
                actualMember.DisplayName);
            Assert.Equal(
                expectedMember.ClassId,
                actualMember.ClassId);
            Assert.Equal(
                expectedMember.Health,
                actualMember.Health);
            Assert.Equal(
                expectedMember.ZeroHitPointPolicy,
                actualMember.ZeroHitPointPolicy);
            Assert.Equal(
                expectedMember.Ammunition,
                actualMember.Ammunition);
        }
    }
}
