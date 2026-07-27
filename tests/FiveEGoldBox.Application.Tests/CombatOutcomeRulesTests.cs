using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class CombatOutcomeRulesTests
{
    [Fact]
    public void Finalize_WithNullSession_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            CombatOutcomeRules.Finalize(null!));
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
            CombatOutcomeRules.Finalize(source));

        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            WatchtowerScenario.ProgressOf(source));
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
            CombatOutcomeRules.Finalize(source));
    }

    [Fact]
    public void Finalize_WhenCompletedEncounterContainsDyingParticipant_ThrowsAndPreservesInput()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState cleric =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.cleric");
        CombatantHealthState original =
            cleric.Health;
        source = WatchtowerCombatOutcomeTestData
            .ReplaceParticipantHealth(
                source,
                cleric.PartyMemberId,
                WatchtowerCombatOutcomeTestData
                    .CreateDyingHealth(
                        cleric.Health.HitPoints
                            .MaximumHitPoints));
        ActiveEncounterState active =
            Assert.IsType<ActiveEncounterState>(
                source.ActiveEncounter);

        Assert.Throws<ArgumentException>(() =>
            CombatOutcomeRules.Finalize(source));

        Assert.Same(active, source.ActiveEncounter);
        Assert.Same(original, cleric.Health);
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

        CombatOutcomeResult result =
            CombatOutcomeRules.Finalize(source);

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
        PartyMemberState cleric =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.cleric");
        int fighterIndex = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == fighter.PartyMemberId);
        int barbarianIndex = Array.FindIndex(
            participants,
            participant => participant.Combatant.CombatantId
                == cleric.PartyMemberId);

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
                    CombatantId = cleric.PartyMemberId
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
            CombatOutcomeRules.Finalize(source));
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
            CombatOutcomeRules.Finalize(source)
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

        PartyMemberState cleric =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                CombatOutcomeRules.Finalize(source)
                    .State,
                "class.cleric");

        Assert.Equal(0, cleric.Health.HitPoints.CurrentHitPoints);
        Assert.True(cleric.Health.DeathSavingThrows.IsStable);
        Assert.False(cleric.Health.IsDead);
    }

    [Fact]
    public void Finalize_PartyVictory_PreservesFailedDeathSaveDeath()
    {
        PartyMemberState casualty =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                CombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession())
                    .State,
                "class.wizard");

        Assert.Equal(3, casualty.Health.DeathSavingThrows.FailureCount);
        Assert.True(casualty.Health.IsDead);
        Assert.False(casualty.Health.IsInstantlyDead);
    }

    [Fact]
    public void Finalize_RaiderVictory_PreservesInstantDeath()
    {
        PartyMemberState casualty =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                CombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreateRaiderVictorySession())
                    .State,
                "class.wizard");

        Assert.True(casualty.Health.IsInstantlyDead);
        Assert.Equal(0, casualty.Health.DeathSavingThrows.FailureCount);
        Assert.False(casualty.Health.DeathSavingThrows.IsStable);
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
            CombatOutcomeRules.Finalize(source));
        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Fact]
    public void Finalize_PartyVictory_ProjectsArcherAmmunitionExactly()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession(
                    archerAmmunition: 2);
        int persistentBefore =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.rogue")
                .Ammunition!.RemainingQuantity;

        PartyMemberState archer =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                CombatOutcomeRules.Finalize(source)
                    .State,
                "class.rogue");

        Assert.NotEqual(2, persistentBefore);
        Assert.Equal(2, archer.Ammunition!.RemainingQuantity);
        Assert.Equal("weapon.shortbow", archer.Ammunition.WeaponId);
        Assert.Equal("item.arrow", archer.Ammunition.AmmunitionItemId);
    }

    [Fact]
    public void Finalize_PartyVictory_ProjectsZeroArcherAmmunition()
    {
        PartyMemberState archer =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                CombatOutcomeRules.Finalize(
                    WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession(
                            archerAmmunition: 0))
                    .State,
                "class.rogue");

        Assert.Equal(0, archer.Ammunition!.RemainingQuantity);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("null-quantity")]
    [InlineData("negative-quantity")]
    [InlineData("wrong-weapon")]
    [InlineData("wrong-item")]
    public void Finalize_WhenArcherAmmunitionAuthorityIsInvalid_ThrowsAndPreservesInput(
        string failure)
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        PartyMemberState archer =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.rogue");
        EncounterParticipantState participant =
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                archer.PartyMemberId);
        WeaponAttack longbow = Assert.Single(
            participant.CombatProfile.WeaponAttacks,
            weapon => weapon.WeaponId == "weapon.shortbow");
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
            CombatOutcomeRules.Finalize(source));
        Assert.Same(originalMembers, source.Party.Members);
        Assert.Same(
            weapons,
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                archer.PartyMemberId)
                .CombatProfile.WeaponAttacks);
    }

    [Fact]
    public void Finalize_DoesNotCreateOrAlterNonArcherAmmunition()
    {
        ApplicationSessionState result =
            CombatOutcomeRules.Finalize(
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
                "class.cleric").Ammunition);
        Assert.NotNull(
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                result,
                "class.rogue").Ammunition);
    }

    [Fact]
    public void Finalize_PreservesAllUnrelatedPartyData()
    {
        ApplicationSessionState source =
            WatchtowerCombatOutcomeTestData
                .CreatePartyVictorySession();
        ApplicationSessionState result =
            CombatOutcomeRules.Finalize(source)
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

        CombatOutcomeResult result =
            CombatOutcomeRules.Finalize(source);

        Assert.Equal(CombatOutcome.PartyVictory, result.Outcome);
        Assert.Equal(ApplicationMode.Exploration, result.ResultingMode);
        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.RaidersDefeated),
            result.ResultingProgressId);
        Assert.Equal("location.ruined-watchtower", result.State.CurrentLocationId);
        Assert.Same(active.ReturnContext, result.State.Exploration);
        Assert.Null(result.State.RegionalTravel);
        Assert.Null(result.State.ActiveEncounter);
        ApplicationSessionRules.Validate(result.State);
    }

    [Fact]
    public void Finalize_RaiderVictory_ReturnsTerminalScenarioConclusionOutcome()
    {
        CombatOutcomeResult result =
            CombatOutcomeRules.Finalize(
                WatchtowerCombatOutcomeTestData
                    .CreateRaiderVictorySession());

        Assert.Equal(CombatOutcome.ScenarioDefeat, result.Outcome);
        Assert.Equal(ApplicationMode.ScenarioConclusion, result.ResultingMode);
        Assert.Equal(
            WatchtowerScenario.ToProgressId(
                WatchtowerScenarioProgress.PartyDefeated),
            result.ResultingProgressId);
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
            CombatOutcomeRules.Finalize(source)
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
        PartyMemberState archer =
            WatchtowerCombatOutcomeTestData.GetPartyMember(
                source,
                "class.rogue");
        EncounterParticipantState participant =
            WatchtowerCombatOutcomeTestData.GetParticipant(
                source,
                archer.PartyMemberId);
        WeaponAttack longbow = Assert.Single(
            participant.CombatProfile.WeaponAttacks,
            weapon => weapon.WeaponId == "weapon.shortbow");
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
            CombatOutcomeRules.Finalize(source));

        Assert.Same(sourceParty, source.Party);
        Assert.Same(sourceEncounter, source.ActiveEncounter);
        Assert.Equal(ApplicationMode.Encounter, source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.SignalActivated,
            WatchtowerScenario.ProgressOf(source));
        Assert.Equal(37, source.RandomValuesConsumed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Finalize_ResultMirrorsCanonicalOutcomeState(
        bool partyVictory)
    {
        CombatOutcomeResult result =
            CombatOutcomeRules.Finalize(
                partyVictory
                    ? WatchtowerCombatOutcomeTestData
                        .CreatePartyVictorySession()
                    : WatchtowerCombatOutcomeTestData
                        .CreateRaiderVictorySession());

        Assert.Equal(result.State.CurrentMode, result.ResultingMode);
        Assert.Equal(
            result.State.Scenario.ProgressId,
            result.ResultingProgressId);
        Assert.Equal(
            partyVictory
                ? CombatOutcome.PartyVictory
                : CombatOutcome.ScenarioDefeat,
            result.Outcome);
    }
}
