using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatOutcomeRulesTests
{
    [Fact]
    public void Finalize_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(null!));
    }

    [Fact]
    public void Finalize_WhenEncounterIsActive_ThrowsAndPreservesInput()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData.CreateActiveSession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        Assert.Throws<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));

        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            source.Scenario.Progress);
        Assert.Same(active, source.ActiveEncounter);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("side.unsupported")]
    public void Finalize_WhenCompletedWinnerIsInvalid_Throws(
        string? winningSideId)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        EncounterState encounter =
            WatchtowerCombatOutcomeTestData.GetEncounter(source)
            with
            {
                WinningSideId = winningSideId
            };
        source = WatchtowerCombatOutcomeTestData.ReplaceEncounter(
            source,
            encounter);

        Assert.Throws<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));
    }

    [Fact]
    public void Finalize_WhenCompletedEncounterContainsDyingParticipant_ThrowsAndPreservesInput()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState barbarian =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.barbarian");
        CombatantHealthState original =
            barbarian.Health;
        source = WatchtowerCombatOutcomeTestData
            .ReplaceParticipantHealth(
                source,
                barbarian.PartyMemberId,
                WatchtowerCombatOutcomeTestData
                    .CreateDyingHealth(
                        barbarian.Health.HitPoints
                            .MaximumHitPoints));
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        Assert.Throws<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));

        Assert.Same(active, source.ActiveEncounter);
        Assert.Same(original, barbarian.Health);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Fact]
    public void Finalize_WhenEncounterParticipantOrderDiffers_MapsByStablePartyMemberId()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        EncounterState encounter =
            WatchtowerCombatOutcomeTestData.GetEncounter(source);
        source = WatchtowerCombatOutcomeTestData.ReplaceEncounter(
            source,
            encounter with
            {
                Participants = Array.AsReadOnly(
                    encounter.Participants.Reverse().ToArray())
            });

        WatchtowerCombatOutcomeResult result =
            WatchtowerCombatOutcomeRules.Finalize(source);

        Assert.Equal(
            source.Party.Members.Select(member =>
                member.PartyMemberId),
            result.State.Party.Members.Select(member =>
                member.PartyMemberId));

        foreach (PartyMemberState member
            in result.State.Party.Members)
        {
            EncounterParticipantState participant =
                WatchtowerCombatOutcomeTestData.GetParticipant(
                    source,
                    member.PartyMemberId);
            Assert.Equal(
                participant.Combatant.Health,
                member.Health);
        }
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("wrong-side")]
    public void Finalize_WhenPartyIdentityMappingIsInvalid_ThrowsAndPreservesInput(
        string failure)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        EncounterState encounter =
            WatchtowerCombatOutcomeTestData.GetEncounter(source);
        EncounterParticipantState[] participants =
            encounter.Participants.ToArray();
        PartyMemberState fighter =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.fighter");
        PartyMemberState barbarian =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.barbarian");
        int fighterIndex = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == fighter.PartyMemberId);
        int barbarianIndex = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == barbarian.PartyMemberId);

        participants[fighterIndex] = failure switch
        {
            "unknown" => participants[fighterIndex] with
            {
                Combatant = participants[fighterIndex]
                    .Combatant with
                {
                    CombatantId = "party-member.unknown"
                }
            },
            "duplicate" => participants[fighterIndex] with
            {
                Combatant = participants[fighterIndex]
                    .Combatant with
                {
                    CombatantId = barbarian.PartyMemberId
                }
            },
            "missing" => participants.Where((_, index) =>
                index != fighterIndex).Append(
                    participants[fighterIndex] with
                    {
                        Combatant = participants[fighterIndex]
                            .Combatant with
                        {
                            CombatantId = "party-member.other"
                        }
                    }).ToArray()[fighterIndex],
            "wrong-side" => participants[fighterIndex] with
            {
                SideId = WatchtowerCombatOutcomeTestData
                    .RaiderSideId
            },
            _ => throw new InvalidOperationException()
        };

        source = WatchtowerCombatOutcomeTestData.ReplaceEncounter(
            source,
            encounter with
            {
                Participants = Array.AsReadOnly(participants)
            });
        IReadOnlyList<PartyMemberState> originalMembers =
            source.Party.Members;

        Assert.ThrowsAny<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));
        Assert.Same(originalMembers, source.Party.Members);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Fact]
    public void Finalize_PartyVictory_ProjectsEveryHealthFieldByStableIdentity()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();

        ApplicationSessionState result =
            WatchtowerCombatOutcomeRules.Finalize(source)
                .State;

        foreach (PartyMemberState member
            in result.Party.Members)
        {
            CombatantHealthState expected =
                WatchtowerCombatOutcomeTestData.GetParticipant(
                    source,
                    member.PartyMemberId)
                    .Combatant.Health;
            Assert.Equal(expected.HitPoints, member.Health.HitPoints);
            Assert.Equal(
                expected.DeathSavingThrows,
                member.Health.DeathSavingThrows);
            Assert.Equal(
                expected.IsInstantlyDead,
                member.Health.IsInstantlyDead);
            Assert.Equal(expected.IsDead, member.Health.IsDead);
        }
    }

    [Fact]
    public void Finalize_PartyVictory_PreservesStableMemberAtZeroHitPoints()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();

        PartyMemberState barbarian =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                WatchtowerCombatOutcomeRules.Finalize(source)
                    .State,
                "class.barbarian");

        Assert.Equal(0, barbarian.Health.HitPoints.CurrentHitPoints);
        Assert.True(barbarian.Health.DeathSavingThrows.IsStable);
        Assert.False(barbarian.Health.IsDead);
    }

    [Fact]
    public void Finalize_PartyVictory_PreservesFailedDeathSaveDeath()
    {
        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                WatchtowerCombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession())
                    .State,
                "class.ranger");

        Assert.Equal(3, ranger.Health.DeathSavingThrows.FailureCount);
        Assert.True(ranger.Health.IsDead);
        Assert.False(ranger.Health.IsInstantlyDead);
    }

    [Fact]
    public void Finalize_RaiderVictory_PreservesInstantDeath()
    {
        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                WatchtowerCombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreateRaiderVictorySession())
                    .State,
                "class.ranger");

        Assert.True(ranger.Health.IsInstantlyDead);
        Assert.Equal(0, ranger.Health.DeathSavingThrows.FailureCount);
        Assert.False(ranger.Health.DeathSavingThrows.IsStable);
    }

    [Theory]
    [InlineData(1, 0, false, false)]
    [InlineData(0, 1, false, false)]
    [InlineData(0, 0, true, false)]
    [InlineData(0, 0, false, true)]
    public void Finalize_WhenPositiveHitPointHealthContainsDeathState_ThrowsAndPreservesInput(
        int successes,
        int failures,
        bool isStable,
        bool isInstantlyDead)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState fighter =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.fighter");
        CombatantHealthState invalid = new()
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = fighter.Health.HitPoints.MaximumHitPoints,
                CurrentHitPoints = 2,
                TemporaryHitPoints = 0
            },
            DeathSavingThrows = new DeathSavingThrowState
            {
                SuccessCount = successes,
                FailureCount = failures,
                IsStable = isStable
            },
            IsInstantlyDead = isInstantlyDead
        };
        source = WatchtowerCombatOutcomeTestData
            .ReplaceParticipantHealth(
                source,
                fighter.PartyMemberId,
                invalid);

        Assert.ThrowsAny<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));
        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Fact]
    public void Finalize_PartyVictory_ProjectsRangerAmmunitionExactly()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession(
                    rangerAmmunition: 2);
        int persistentBefore =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.ranger")
                .Ammunition!.RemainingQuantity;

        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                WatchtowerCombatOutcomeRules.Finalize(source)
                    .State,
                "class.ranger");

        Assert.NotEqual(2, persistentBefore);
        Assert.Equal(2, ranger.Ammunition!.RemainingQuantity);
        Assert.Equal("weapon.longbow", ranger.Ammunition.WeaponId);
        Assert.Equal("item.arrow", ranger.Ammunition.AmmunitionItemId);
    }

    [Fact]
    public void Finalize_PartyVictory_ProjectsZeroRangerAmmunition()
    {
        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                WatchtowerCombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession(
                            rangerAmmunition: 0))
                    .State,
                "class.ranger");

        Assert.Equal(0, ranger.Ammunition!.RemainingQuantity);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("null-quantity")]
    [InlineData("negative-quantity")]
    [InlineData("wrong-weapon")]
    [InlineData("wrong-item")]
    public void Finalize_WhenRangerAmmunitionAuthorityIsInvalid_ThrowsAndPreservesInput(
        string failure)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.ranger");
        EncounterParticipantState participant =
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                ranger.PartyMemberId);
        WeaponAttack longbow = Assert.Single(
            participant.CombatProfile.WeaponAttacks,
            weapon => weapon.WeaponId == "weapon.longbow");
        IReadOnlyList<WeaponAttack> weapons = failure switch
        {
            "missing" => Array.Empty<WeaponAttack>(),
            "duplicate" => Array.AsReadOnly(
                new[] { longbow, longbow with { WeaponName = "Duplicate" } }),
            "null-quantity" => Array.AsReadOnly(
                new[] { longbow with { AmmunitionQuantityAvailable = null } }),
            "negative-quantity" => Array.AsReadOnly(
                new[] { longbow with { AmmunitionQuantityAvailable = -1 } }),
            "wrong-weapon" => Array.AsReadOnly(
                new[] { longbow with { WeaponId = "weapon.other" } }),
            "wrong-item" => Array.AsReadOnly(
                new[] { longbow with { AmmunitionItemId = "item.other" } }),
            _ => throw new InvalidOperationException()
        };
        source = WatchtowerCombatOutcomeTestData.ReplaceParticipant(
            source,
            participant with
            {
                CombatProfile = participant.CombatProfile with
                {
                    WeaponAttacks = weapons
                }
            });
        IReadOnlyList<PartyMemberState> originalMembers =
            source.Party.Members;

        Assert.ThrowsAny<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));
        Assert.Same(originalMembers, source.Party.Members);
        Assert.Same(
            weapons,
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                ranger.PartyMemberId)
                .CombatProfile.WeaponAttacks);
    }

    [Fact]
    public void Finalize_DoesNotCreateOrAlterNonRangerAmmunition()
    {
        ApplicationSessionState result =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreatePartyVictorySession())
                .State;

        Assert.Null(
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                result,
                "class.fighter").Ammunition);
        Assert.Null(
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                result,
                "class.barbarian").Ammunition);
        Assert.NotNull(
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                result,
                "class.ranger").Ammunition);
    }

    [Fact]
    public void Finalize_PreservesAllUnrelatedPartyData()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        ApplicationSessionState result =
            WatchtowerCombatOutcomeRules.Finalize(source)
                .State;

        Assert.Equal(source.Party.PartyId, result.Party.PartyId);
        Assert.Equal(source.Party.Members.Count, result.Party.Members.Count);

        for (int index = 0; index < source.Party.Members.Count; index++)
        {
            PartyMemberState before = source.Party.Members[index];
            PartyMemberState after = result.Party.Members[index];
            Assert.Equal(before.PartyMemberId, after.PartyMemberId);
            Assert.Equal(before.CharacterDefinitionId, after.CharacterDefinitionId);
            Assert.Equal(before.DisplayName, after.DisplayName);
            Assert.Equal(before.ClassId, after.ClassId);
            Assert.Equal(before.ZeroHitPointPolicy, after.ZeroHitPointPolicy);
        }
    }

    [Fact]
    public void Finalize_PartyVictory_ReturnsExplorationOutcomeAndExactReturnContext()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        WatchtowerCombatOutcomeResult result =
            WatchtowerCombatOutcomeRules.Finalize(source);

        Assert.Equal(WatchtowerCombatOutcome.PartyVictory, result.Outcome);
        Assert.Equal(ApplicationMode.Exploration, result.ResultingMode);
        Assert.Equal(
            WatchtowerScenarioProgress.RaidersDefeated,
            result.ResultingProgress);
        Assert.Equal("location.ruined-watchtower", result.State.CurrentLocationId);
        Assert.Same(active.ReturnContext, result.State.Exploration);
        Assert.Null(result.State.RegionalTravel);
        Assert.Null(result.State.ActiveEncounter);
        ApplicationSessionRules.Validate(result.State);
    }

    [Fact]
    public void Finalize_RaiderVictory_ReturnsTerminalScenarioConclusionOutcome()
    {
        WatchtowerCombatOutcomeResult result =
            WatchtowerCombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreateRaiderVictorySession());

        Assert.Equal(WatchtowerCombatOutcome.ScenarioDefeat, result.Outcome);
        Assert.Equal(ApplicationMode.ScenarioConclusion, result.ResultingMode);
        Assert.Equal(
            WatchtowerScenarioProgress.PartyDefeated,
            result.ResultingProgress);
        Assert.Equal("location.ruined-watchtower", result.State.CurrentLocationId);
        Assert.Null(result.State.Exploration);
        Assert.Null(result.State.RegionalTravel);
        Assert.Null(result.State.ActiveEncounter);
        ApplicationSessionRules.Validate(result.State);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Finalize_ForEitherSupportedWinner_PreservesRandomSeedAndCursor(
        bool partyVictory)
    {
        ApplicationSessionState source = partyVictory
            ? WatchtowerCombatOutcomeTestData.CreatePartyVictorySession()
            : WatchtowerCombatOutcomeTestData.CreateRaiderVictorySession();

        ApplicationSessionState result =
            WatchtowerCombatOutcomeRules.Finalize(source)
                .State;

        Assert.Equal(source.RandomSeed, result.RandomSeed);
        Assert.Equal(
            source.RandomValuesConsumed,
            result.RandomValuesConsumed);
    }

    [Fact]
    public void Finalize_WhenLateValidationFails_DoesNotPartiallyProjectOrTransition()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState ranger =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.ranger");
        EncounterParticipantState participant =
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                ranger.PartyMemberId);
        WeaponAttack longbow = Assert.Single(
            participant.CombatProfile.WeaponAttacks,
            weapon => weapon.WeaponId == "weapon.longbow");
        source = WatchtowerCombatOutcomeTestData.ReplaceParticipant(
            source,
            participant with
            {
                CombatProfile = participant.CombatProfile with
                {
                    WeaponAttacks = Array.AsReadOnly(
                        new[]
                        {
                            longbow with
                            {
                                AmmunitionQuantityAvailable = null
                            }
                        })
                }
            });
        PartyState sourceParty = source.Party;
        ActiveEncounterState sourceEncounter =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        Assert.Throws<ArgumentException>(() =>
            WatchtowerCombatOutcomeRules.Finalize(source));

        Assert.Same(sourceParty, source.Party);
        Assert.Same(sourceEncounter, source.ActiveEncounter);
        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            source.Scenario.Progress);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Finalize_ResultMirrorsCanonicalOutcomeState(
        bool partyVictory)
    {
        WatchtowerCombatOutcomeResult result =
            WatchtowerCombatOutcomeRules.Finalize(
                partyVictory
                    ? WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession()
                    : WatchtowerCombatOutcomeTestData
                        .CreateRaiderVictorySession());

        Assert.Equal(result.State.CurrentMode, result.ResultingMode);
        Assert.Equal(
            result.State.Scenario.Progress,
            result.ResultingProgress);
        Assert.Equal(
            partyVictory
                ? WatchtowerCombatOutcome.PartyVictory
                : WatchtowerCombatOutcome.ScenarioDefeat,
            result.Outcome);
    }
}
