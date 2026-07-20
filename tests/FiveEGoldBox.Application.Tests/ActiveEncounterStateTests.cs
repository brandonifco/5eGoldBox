using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ActiveEncounterStateTests
{
    [Fact]
    public void Validate_EncounterModeWithoutActiveEncounter_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession() with
            {
                ActiveEncounter = null
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterModeWithRootExploration_Throws()
    {
        ApplicationSessionState encounter =
            WatchtowerSignalTestData.CreateEncounterSession();
        ApplicationSessionState state = encounter with
        {
            Exploration =
                Assert.IsType<ActiveEncounterState>(
                    encounter.ActiveEncounter)
                    .ReturnContext
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterModeWithRegionalTravel_Throws()
    {
        ApplicationSessionState encounter =
            WatchtowerSignalTestData.CreateEncounterSession();
        ApplicationSessionState travel =
            RegionalTravelRules.BeginWatchtowerJourney(
                WatchtowerSignalTestData.CreateAcceptedSession());
        ApplicationSessionState state = encounter with
        {
            RegionalTravel = travel.RegionalTravel
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_NonEncounterModeWithActiveEncounter_Throws()
    {
        ApplicationSessionState encounter =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                encounter.ActiveEncounter);
        ApplicationSessionState exploration =
            WatchtowerSignalTestData.CreateSignalReadySession()
                with
                {
                    ActiveEncounter = active
                };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(exploration));
    }

    [Fact]
    public void Validate_OutpostModeWithActiveEncounter_Throws()
    {
        ApplicationSessionState encounter =
            WatchtowerSignalTestData.CreateEncounterSession();
        ApplicationSessionState state = encounter with
        {
            CurrentMode = ApplicationMode.Outpost
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_RegionalTravelModeWithActiveEncounter_Throws()
    {
        ApplicationSessionState encounter =
            WatchtowerSignalTestData.CreateEncounterSession();
        ApplicationSessionState travel =
            RegionalTravelRules.BeginWatchtowerJourney(
                WatchtowerSignalTestData.CreateAcceptedSession());
        ApplicationSessionState state = travel with
        {
            ActiveEncounter = encounter.ActiveEncounter
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterAtWrongLocation_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession() with
            {
                CurrentLocationId = "location.outpost"
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterAtWrongScenarioProgress_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress =
                        WatchtowerScenarioProgress
                            .MissionAccepted
                }
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithNullReturnContext_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                ReturnContext = null!
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithInvalidReturnContext_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                ReturnContext = active.ReturnContext with
                {
                    Facing =
                        ExplorationFacing.North
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithNullCoreEncounter_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = null!
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithWrongEncounterId_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    EncounterId = "encounter.other"
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Theory]
    [InlineData("side.party")]
    [InlineData("side.watchtower-raiders")]
    public void Validate_EncounterWithCompletedCoreEncounter_Succeeds(
        string winningSideId)
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);
        EncounterState completed = EncounterRules.Complete(
            active.Encounter,
            winningSideId);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = completed
            }
        };

        ApplicationSessionRules.Validate(state);
    }

    [Fact]
    public void Validate_CompletedEncounterWithoutWinner_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);
        EncounterState completedWithoutWinner = active.Encounter with
        {
            LifecycleState = EncounterLifecycleState.Completed,
            WinningSideId = null
        };

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = completedWithoutWinner
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_CompletedEncounterWithUnsupportedWinner_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);
        EncounterState completed = active.Encounter with
        {
            LifecycleState = EncounterLifecycleState.Completed,
            WinningSideId = "side.other"
        };

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = completed
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_ActiveEncounterWithWinningSide_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    WinningSideId = "side.party"
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithWrongParticipantCount_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    Participants = active.Encounter
                        .Participants
                        .Take(4)
                        .ToArray()
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithUnknownPartyParticipant_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);
        EncounterParticipantState[] participants =
            active.Encounter.Participants.ToArray();

        participants[0] = participants[0] with
        {
            Combatant = participants[0].Combatant with
            {
                CombatantId = "party-member.unknown"
            }
        };

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    Participants = participants
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_EncounterWithWrongBattlefieldId_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);

        state = state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    Battlefield =
                        active.Encounter.Battlefield with
                        {
                            BattlefieldId =
                                "battlefield.other"
                        }
                }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_PartyParticipantAssignedToRaiderSide_Throws()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        string partyMemberId =
            state.Party.Members[0].PartyMemberId;

        state = WithParticipantSide(
            state,
            partyMemberId,
            "side.watchtower-raiders");

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Theory]
    [InlineData("combatant.watchtower-raider.melee")]
    [InlineData("combatant.watchtower-raider.ranged")]
    public void Validate_RaiderParticipantAssignedToPartySide_Throws(
        string combatantId)
    {
        ApplicationSessionState state =
            WithParticipantSide(
                WatchtowerSignalTestData
                    .CreateEncounterSession(),
                combatantId,
                "side.party");

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    private static ApplicationSessionState WithParticipantSide(
        ApplicationSessionState state,
        string combatantId,
        string sideId)
    {
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                state.ActiveEncounter);
        EncounterParticipantState[] participants =
            active.Encounter.Participants.ToArray();
        int index = Array.FindIndex(
            participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));

        Assert.True(index >= 0);

        participants[index] = participants[index] with
        {
            SideId = sideId
        };

        return state with
        {
            ActiveEncounter = active with
            {
                Encounter = active.Encounter with
                {
                    Participants = participants
                }
            }
        };
    }
}
