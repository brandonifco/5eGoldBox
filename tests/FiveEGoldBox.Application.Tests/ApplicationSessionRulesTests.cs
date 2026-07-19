using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class ApplicationSessionRulesTests
{
    [Fact]
    public void CreateNew_WithValidInputs_CreatesOutpostSession()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(
            ApplicationMode.Outpost,
            state.CurrentMode);
    }

    [Fact]
    public void CreateNew_PreservesScenarioAndLocationIdentity()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(
            "scenario.watchtower",
            state.ScenarioId);
        Assert.Equal(
            "location.outpost",
            state.CurrentLocationId);
    }

    [Fact]
    public void CreateNew_PreservesPartyId()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(
            "party.player",
            state.Party.PartyId);
    }

    [Fact]
    public void CreateNew_PreservesOrderedStableMemberIds()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(
            new[]
            {
                "party-member.fighter",
                "party-member.barbarian",
                "party-member.ranger"
            },
            state.Party.Members.Select(
                member => member.PartyMemberId));
    }

    [Fact]
    public void CreateNew_PreservesHealthState()
    {
        ApplicationSessionState state = CreateValidSession();

        CombatantHealthState health =
            state.Party.Members[0].Health;

        Assert.Equal(
            12,
            health.HitPoints.MaximumHitPoints);
        Assert.Equal(
            8,
            health.HitPoints.CurrentHitPoints);
        Assert.Equal(
            2,
            health.HitPoints.TemporaryHitPoints);
        Assert.Equal(
            0,
            health.DeathSavingThrows.SuccessCount);
        Assert.Equal(
            0,
            health.DeathSavingThrows.FailureCount);
        Assert.False(health.DeathSavingThrows.IsStable);
        Assert.False(health.IsInstantlyDead);
    }

    [Fact]
    public void CreateNew_PreservesRangerAmmunition()
    {
        ApplicationSessionState state = CreateValidSession();

        AmmunitionState? ammunition =
            state.Party.Members[2].Ammunition;

        Assert.NotNull(ammunition);
        Assert.Equal(
            "weapon.longbow",
            ammunition.WeaponId);
        Assert.Equal(
            "item.arrow",
            ammunition.AmmunitionItemId);
        Assert.Equal(
            18,
            ammunition.RemainingQuantity);
    }

    [Fact]
    public void CreateNew_PreservesRandomSeed()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(8675309, state.RandomSeed);
    }

    [Fact]
    public void CreateNew_WithNegativeRandomSeed_PreservesRandomSeed()
    {
        ApplicationSessionState state =
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId: "location.outpost",
                party: CreateParty(CreateValidMembers()),
                randomSeed: -1);

        Assert.Equal(-1, state.RandomSeed);
    }

    [Fact]
    public void CreateNew_InitializesRandomValuesConsumed()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(0, state.RandomValuesConsumed);
    }

    [Fact]
    public void CreateNew_InitializesMissionNotAcceptedProgress()
    {
        ApplicationSessionState state = CreateValidSession();

        Assert.Equal(
            WatchtowerScenarioProgress
                .MissionNotAccepted,
            state.Scenario.Progress);
    }

    [Fact]
    public void CreateNew_ProtectsMembersFromSourceCollectionMutation()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        PartyState party = CreateParty(members);

        ApplicationSessionState state =
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId: "location.outpost",
                party,
                randomSeed: 8675309);

        members[0] = members[1];

        Assert.NotSame(members, state.Party.Members);
        Assert.Equal(
            "party-member.fighter",
            state.Party.Members[0].PartyMemberId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankScenarioId_Throws(
        string scenarioId)
    {
        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.CreateNew(
                scenarioId,
                currentLocationId: "location.outpost",
                CreateParty(CreateValidMembers()),
                randomSeed: 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankLocationId_Throws(
        string currentLocationId)
    {
        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId,
                CreateParty(CreateValidMembers()),
                randomSeed: 1));
    }

    [Fact]
    public void CreateNew_WithNullParty_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationSessionRules.CreateNew(
                scenarioId: "scenario.watchtower",
                currentLocationId: "location.outpost",
                party: null!,
                randomSeed: 1));
    }

    [Fact]
    public void CreateNew_WithNullMemberCollection_Throws()
    {
        PartyState party = new()
        {
            PartyId = "party.player",
            Members = null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            CreateSession(party));
    }

    [Fact]
    public void CreateNew_WithNullMemberEntry_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[1] = null!;

        Assert.Throws<ArgumentNullException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankPartyId_Throws(
        string partyId)
    {
        PartyState party = CreateParty(
            CreateValidMembers()) with
        {
            PartyId = partyId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(party));
    }

    [Fact]
    public void CreateNew_WithDuplicateMemberId_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[1] = members[1] with
        {
            PartyMemberId = members[0].PartyMemberId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithDuplicateCharacterDefinitionId_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[1] = members[1] with
        {
            CharacterDefinitionId =
                members[0].CharacterDefinitionId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithWrongPartySize_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers()[..2];

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithoutFighter_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            ClassId = "class.ranger",
            Ammunition = CreateAmmunition()
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithoutBarbarian_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[1] = members[1] with
        {
            ClassId = "class.fighter"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithoutRanger_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[2] = members[2] with
        {
            ClassId = "class.barbarian",
            Ammunition = null
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithDuplicateBoundedClassRole_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[1] = members[1] with
        {
            ClassId = "class.fighter"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithUnsupportedClassId_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            ClassId = "class.wizard"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankPartyMemberId_Throws(
        string partyMemberId)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            PartyMemberId = partyMemberId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankCharacterDefinitionId_Throws(
        string characterDefinitionId)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            CharacterDefinitionId =
                characterDefinitionId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankDisplayName_Throws(
        string displayName)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            DisplayName = displayName
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankClassId_Throws(
        string classId)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            ClassId = classId
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithUndefinedZeroHitPointPolicy_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            ZeroHitPointPolicy =
                (CombatantZeroHitPointPolicy)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithNullHealth_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            Health = null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithNullHitPointState_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                HitPoints = null!
            });
    }

    [Fact]
    public void CreateNew_WithNullDeathSavingThrowState_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                DeathSavingThrows = null!
            });
    }

    [Fact]
    public void CreateNew_WithInvalidMaximumHitPoints_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                HitPoints = health.HitPoints with
                {
                    MaximumHitPoints = 0,
                    CurrentHitPoints = 0
                }
            });
    }

    [Fact]
    public void CreateNew_WithNegativeCurrentHitPoints_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                HitPoints = health.HitPoints with
                {
                    CurrentHitPoints = -1
                }
            });
    }

    [Fact]
    public void CreateNew_WithCurrentHitPointsAboveMaximum_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                HitPoints = health.HitPoints with
                {
                    CurrentHitPoints =
                        health.HitPoints.MaximumHitPoints + 1
                }
            });
    }

    [Fact]
    public void CreateNew_WithNegativeTemporaryHitPoints_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                HitPoints = health.HitPoints with
                {
                    TemporaryHitPoints = -1
                }
            });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void CreateNew_WithInvalidDeathSavingThrowSuccessCount_Throws(
        int successCount)
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        SuccessCount = successCount
                    }
            });
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void CreateNew_WithInvalidDeathSavingThrowFailureCount_Throws(
        int failureCount)
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        FailureCount = failureCount
                    }
            });
    }

    [Fact]
    public void CreateNew_WithStableHealthAndDeathSaveProgress_Throws()
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        SuccessCount = 1,
                        IsStable = true
                    }
            });
    }

    [Fact]
    public void CreateNew_WithStableAndDeadHealth_Throws()
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        FailureCount =
                            DeathSavingThrowRules
                                .FailuresRequired,
                        IsStable = true
                    }
            });
    }

    [Fact]
    public void CreateNew_WithHitPointsAndDeathSaveProgress_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        SuccessCount = 1
                    }
            });
    }

    [Fact]
    public void CreateNew_WithInstantDeathAboveZeroHitPoints_Throws()
    {
        AssertInvalidHealthThrows(
            health => health with
            {
                IsInstantlyDead = true
            });
    }

    [Fact]
    public void CreateNew_WithInstantDeathAndStableHealth_Throws()
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        IsStable = true
                    },
                IsInstantlyDead = true
            });
    }

    [Fact]
    public void CreateNew_WithBothInstantDeathAndFailedDeathSaves_Throws()
    {
        AssertInvalidHealthThrows(
            health => CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    health.DeathSavingThrows with
                    {
                        FailureCount =
                            DeathSavingThrowRules
                                .FailuresRequired
                    },
                IsInstantlyDead = true
            });
    }

    [Fact]
    public void CreateNew_WithDefeatedPolicyAndDeathSaveProgress_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy.Defeated,
            Health = CreateHealthAtZero() with
            {
                DeathSavingThrows =
                    DeathSavingThrowRules.Create() with
                    {
                        SuccessCount = 1
                    }
            }
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankAmmunitionWeaponId_Throws(
        string weaponId)
    {
        AssertInvalidAmmunitionThrows(
            ammunition => ammunition with
            {
                WeaponId = weaponId
            });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void CreateNew_WithBlankAmmunitionItemId_Throws(
        string ammunitionItemId)
    {
        AssertInvalidAmmunitionThrows(
            ammunition => ammunition with
            {
                AmmunitionItemId = ammunitionItemId
            });
    }

    [Fact]
    public void CreateNew_WithNegativeAmmunition_Throws()
    {
        AssertInvalidAmmunitionThrows(
            ammunition => ammunition with
            {
                RemainingQuantity = -1
            });
    }

    [Fact]
    public void CreateNew_WithoutRangerAmmunition_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[2] = members[2] with
        {
            Ammunition = null
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void CreateNew_WithAmmunitionOnNonRanger_Throws()
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            Ammunition = CreateAmmunition()
        };

        Assert.Throws<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    [Fact]
    public void Validate_WithNullScenario_Throws()
    {
        ApplicationSessionState state =
            CreateValidSession() with
            {
                Scenario = null!
            };

        Assert.Throws<ArgumentNullException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_WithUndefinedScenarioProgress_Throws()
    {
        ApplicationSessionState state =
            CreateValidSession() with
            {
                Scenario = new WatchtowerScenarioState
                {
                    Progress =
                        (WatchtowerScenarioProgress)999
                }
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_WithNegativeRandomValuesConsumed_Throws()
    {
        ApplicationSessionState state =
            CreateValidSession() with
            {
                RandomValuesConsumed = -1
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_WithUndefinedApplicationMode_Throws()
    {
        ApplicationSessionState state =
            CreateValidSession() with
            {
                CurrentMode = (ApplicationMode)999
            };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    [Fact]
    public void Validate_WithNonOutpostApplicationMode_Throws()
    {
        ApplicationSessionState state =
            CreateValidSession() with
            {
                CurrentMode = ApplicationMode.Exploration
            };

        Assert.Throws<ArgumentException>(() =>
            ApplicationSessionRules.Validate(state));
    }

    private static void AssertInvalidHealthThrows(
        Func<CombatantHealthState, CombatantHealthState>
            changeHealth)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        members[0] = members[0] with
        {
            Health = changeHealth(members[0].Health)
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    private static void AssertInvalidAmmunitionThrows(
        Func<AmmunitionState, AmmunitionState>
            changeAmmunition)
    {
        PartyMemberState[] members =
            CreateValidMembers();
        AmmunitionState ammunition =
            Assert.IsType<AmmunitionState>(
                members[2].Ammunition);
        members[2] = members[2] with
        {
            Ammunition = changeAmmunition(ammunition)
        };

        Assert.ThrowsAny<ArgumentException>(() =>
            CreateSession(CreateParty(members)));
    }

    private static ApplicationSessionState
        CreateValidSession()
    {
        return CreateSession(
            CreateParty(CreateValidMembers()));
    }

    private static ApplicationSessionState CreateSession(
        PartyState party)
    {
        return ApplicationSessionRules.CreateNew(
            scenarioId: "scenario.watchtower",
            currentLocationId: "location.outpost",
            party,
            randomSeed: 8675309);
    }

    private static PartyState CreateParty(
        IReadOnlyList<PartyMemberState> members)
    {
        return new PartyState
        {
            PartyId = "party.player",
            Members = members
        };
    }

    private static PartyMemberState[]
        CreateValidMembers()
    {
        CombatantHealthState fighterHealth =
            CombatantHealthRules.Create(
                maximumHitPoints: 12) with
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 12,
                    CurrentHitPoints = 8,
                    TemporaryHitPoints = 2
                }
            };

        return
        [
            CreateMember(
                partyMemberId:
                    "party-member.fighter",
                characterDefinitionId:
                    "character.fighter",
                displayName: "Fighter",
                classId: "class.fighter",
                maximumHitPoints: 12) with
            {
                Health = fighterHealth
            },
            CreateMember(
                partyMemberId:
                    "party-member.barbarian",
                characterDefinitionId:
                    "character.barbarian",
                displayName: "Barbarian",
                classId: "class.barbarian",
                maximumHitPoints: 14),
            CreateMember(
                partyMemberId:
                    "party-member.ranger",
                characterDefinitionId:
                    "character.ranger",
                displayName: "Ranger",
                classId: "class.ranger",
                maximumHitPoints: 11) with
            {
                Ammunition = CreateAmmunition()
            }
        ];
    }

    private static PartyMemberState CreateMember(
        string partyMemberId,
        string characterDefinitionId,
        string displayName,
        string classId,
        int maximumHitPoints)
    {
        return new PartyMemberState
        {
            PartyMemberId = partyMemberId,
            CharacterDefinitionId =
                characterDefinitionId,
            DisplayName = displayName,
            ClassId = classId,
            ZeroHitPointPolicy =
                CombatantZeroHitPointPolicy
                    .DeathSavingThrows,
            Health = CombatantHealthRules.Create(
                maximumHitPoints),
            Ammunition = null
        };
    }

    private static AmmunitionState CreateAmmunition()
    {
        return new AmmunitionState
        {
            WeaponId = "weapon.longbow",
            AmmunitionItemId = "item.arrow",
            RemainingQuantity = 18
        };
    }

    private static CombatantHealthState
        CreateHealthAtZero()
    {
        return CombatantHealthRules.Create(
            maximumHitPoints: 12) with
        {
            HitPoints = new HitPointState
            {
                MaximumHitPoints = 12,
                CurrentHitPoints = 0,
                TemporaryHitPoints = 0
            }
        };
    }
}
