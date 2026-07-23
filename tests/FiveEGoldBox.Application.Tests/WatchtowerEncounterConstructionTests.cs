using System.Buffers.Binary;
using System.Security.Cryptography;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Encounters;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerEncounterConstructionTests
{
    [Fact]
    public void WatchtowerEncounter_ActivationCreatesAuthoredInitialState()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = GetEncounter(state);

        Assert.Equal(
            "encounter.watchtower-signal-ambush",
            encounter.EncounterId);
        Assert.Equal(
            EncounterLifecycleState.Active,
            encounter.LifecycleState);
        Assert.Null(encounter.WinningSideId);
        Assert.Equal(5, encounter.Participants.Count);
        Assert.Equal(5, encounter.InitiativeOrder.Count);
        Assert.Contains(
            encounter.Participants,
            participant => string.Equals(
                participant.Combatant.CombatantId,
                encounter.ActiveCombatantId,
                StringComparison.Ordinal));
        Assert.Equal(
            "battlefield.watchtower-signal-chamber",
            encounter.Battlefield.BattlefieldId);
        Assert.Equal(5, encounter.Battlefield.Width);
        Assert.Equal(4, encounter.Battlefield.Height);
        Assert.Empty(encounter.Battlefield.BlockedPositions);
        Assert.Empty(encounter.Battlefield.CoverPositions);
        Assert.Empty(
            encounter.Battlefield.DifficultTerrainPositions);
    }

    [Fact]
    public void WatchtowerEncounter_ContainsAuthoredIdentitiesSidesAndDistinctPositions()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = GetEncounter(state);

        string[] expectedPartyIds = state.Party.Members
            .Select(member => member.PartyMemberId)
            .ToArray();

        Assert.All(
            expectedPartyIds,
            partyId => Assert.Equal(
                "side.party",
                FindParticipant(encounter, partyId)
                    .SideId));
        Assert.Equal(
            "side.watchtower-raiders",
            FindParticipant(
                encounter,
                "combatant.watchtower-raider.melee")
                .SideId);
        Assert.Equal(
            "side.watchtower-raiders",
            FindParticipant(
                encounter,
                "combatant.watchtower-raider.ranged")
                .SideId);

        Assert.Equal(
            5,
            encounter.Participants
                .Select(participant => participant.Position)
                .Distinct()
                .Count());
        Assert.Equal(
            new GridPosition(1, 1),
            FindParticipant(
                encounter,
                expectedPartyIds[0]).Position);
        Assert.Equal(
            new GridPosition(1, 2),
            FindParticipant(
                encounter,
                expectedPartyIds[1]).Position);
        Assert.Equal(
            new GridPosition(0, 2),
            FindParticipant(
                encounter,
                expectedPartyIds[2]).Position);
        Assert.Equal(
            new GridPosition(2, 1),
            FindParticipant(
                encounter,
                "combatant.watchtower-raider.melee")
                .Position);
        Assert.Equal(
            new GridPosition(4, 2),
            FindParticipant(
                encounter,
                "combatant.watchtower-raider.ranged")
                .Position);
    }

    [Fact]
    public void WatchtowerEncounter_MapsPersistentPartyHealthAndResolvedProfiles()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = GetEncounter(state);
        ValidatedRuleset ruleset =
            WatchtowerSignalTestData.CreateRuleset();
        CharacterResolver resolver = new(ruleset);

        foreach (PartyMemberState member
            in state.Party.Members)
        {
            EncounterParticipantState participant =
                FindParticipant(
                    encounter,
                    member.PartyMemberId);
            CharacterSnapshot expected =
                resolver.Resolve(
                    WatchtowerSignalTestData
                        .CreateExpectedDraft(member));

            Assert.Equal(
                member.Health,
                participant.Combatant.Health);
            Assert.Equal(
                member.ZeroHitPointPolicy,
                participant.Combatant
                    .ZeroHitPointPolicy);
            Assert.Equal(
                expected.ArmorClass!.Value,
                participant.CombatProfile.ArmorClass);
            Assert.Equal(
                expected.SpeedFeet!.Value,
                participant.TurnResources
                    .MovementSpeedFeet);
            AssertWeaponAttacksEquivalent(
                expected.WeaponAttacks,
                participant.CombatProfile.WeaponAttacks);
            Assert.Equal(
                expected.SavingThrowBonuses.ToArray(),
                participant.CombatProfile
                    .SavingThrowBonuses.ToArray());
            Assert.Equal(
                expected.DamageResponses.ToArray(),
                participant.CombatProfile
                    .DamageResponses.ToArray());
        }
    }

    [Fact]
    public void WatchtowerEncounter_PreservesStableAndDyingPartyHealth()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData
                .CreateSignalReadySessionWithStableAndDyingParty();
        ApplicationSessionState result =
            SignalMechanismRules.Activate(
                source,
                WatchtowerSignalTestData.CreateRuleset());
        EncounterState encounter = GetEncounter(result);
        PartyMemberState stableMember = source.Party.Members[0];
        PartyMemberState dyingMember = source.Party.Members[1];
        EncounterParticipantState stableParticipant =
            FindParticipant(
                encounter,
                stableMember.PartyMemberId);
        EncounterParticipantState dyingParticipant =
            FindParticipant(
                encounter,
                dyingMember.PartyMemberId);

        Assert.Equal(
            stableMember.Health,
            stableParticipant.Combatant.Health);
        Assert.Equal(
            CombatantLifecycleState.Stable,
            stableParticipant.Combatant.LifecycleState);
        Assert.Equal(
            dyingMember.Health,
            dyingParticipant.Combatant.Health);
        Assert.Equal(
            CombatantLifecycleState.Dying,
            dyingParticipant.Combatant.LifecycleState);
        Assert.Equal(
            1,
            dyingParticipant.Combatant.Health
                .DeathSavingThrows.SuccessCount);
        Assert.Equal(
            2,
            dyingParticipant.Combatant.Health
                .DeathSavingThrows.FailureCount);
        Assert.Equal(
            EncounterLifecycleState.Active,
            encounter.LifecycleState);
    }

    [Theory]
    [InlineData(TerminalHealthKind.FailedDeathSaves)]
    [InlineData(TerminalHealthKind.InstantDeath)]
    public void WatchtowerEncounter_TerminalPartyMemberIsRejectedWithoutMutatingSource(
        TerminalHealthKind terminalHealthKind)
    {
        bool isInstantlyDead =
            terminalHealthKind
                == TerminalHealthKind.InstantDeath;
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateSignalReadySession();
        PartyMemberState[] members =
            source.Party.Members.ToArray();
        members[0] = members[0] with
        {
            Health = new CombatantHealthState
            {
                HitPoints = new HitPointState
                {
                    MaximumHitPoints = 12,
                    CurrentHitPoints = 0,
                    TemporaryHitPoints = 0
                },
                DeathSavingThrows =
                    new DeathSavingThrowState
                    {
                        SuccessCount = 0,
                        FailureCount = isInstantlyDead
                            ? 0
                            : DeathSavingThrowRules
                                .FailuresRequired,
                        IsStable = false
                    },
                IsInstantlyDead = isInstantlyDead
            }
        };
        source = source with
        {
            Party = source.Party with
            {
                Members = Array.AsReadOnly(members)
            }
        };
        PartyMemberState[] originalMembers =
            source.Party.Members.ToArray();
        ExplorationState originalExploration =
            Assert.IsType<ExplorationState>(
                source.Exploration);
        int originalRandomValuesConsumed =
            source.RandomValuesConsumed;

        ApplicationSessionRules.Validate(source);
        Assert.True(source.Party.Members[0].Health.IsDead);

        Assert.Throws<ArgumentException>(() =>
            SignalMechanismRules.Activate(
                source,
                WatchtowerSignalTestData.CreateRuleset()));

        Assert.Equal(
            ApplicationMode.Exploration,
            source.CurrentMode);
        Assert.Equal(
            WatchtowerScenarioProgress.MissionAccepted,
            source.Scenario.Progress);
        Assert.Null(source.ActiveEncounter);
        Assert.Equal(
            originalRandomValuesConsumed,
            source.RandomValuesConsumed);
        Assert.Equal(
            "party.player",
            source.Party.PartyId);
        Assert.Equal(
            originalMembers,
            source.Party.Members.ToArray());
        Assert.Equal(
            originalExploration,
            source.Exploration);
    }

    [Fact]
    public void WatchtowerEncounter_MapsRangerAmmunition()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        PartyMemberState ranger = state.Party.Members.Single(
            member => string.Equals(
                member.ClassId,
                "class.ranger",
                StringComparison.Ordinal));
        EncounterParticipantState participant =
            FindParticipant(
                GetEncounter(state),
                ranger.PartyMemberId);
        WeaponAttack weapon =
            Assert.Single(
                participant.CombatProfile
                    .WeaponAttacks);

        Assert.Equal("weapon.longbow", weapon.WeaponId);
        Assert.Equal("item.arrow", weapon.AmmunitionItemId);
        Assert.Equal(
            ranger.Ammunition!.RemainingQuantity,
            weapon.AmmunitionQuantityAvailable);
    }

    [Fact]
    public void WatchtowerEncounter_CreatesFixedMeleeRaiderProfile()
    {
        EncounterParticipantState raider = FindParticipant(
            GetEncounter(
                WatchtowerSignalTestData
                    .CreateEncounterSession()),
            "combatant.watchtower-raider.melee");
        WeaponAttack weapon = Assert.Single(
            raider.CombatProfile.WeaponAttacks);

        Assert.Equal(
            "combatant.watchtower-raider.melee",
            raider.Combatant.CombatantId);
        Assert.Equal(
            "side.watchtower-raiders",
            raider.SideId);
        Assert.Equal(
            9,
            raider.Combatant.Health.HitPoints
                .MaximumHitPoints);
        Assert.Equal(
            9,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            CombatantZeroHitPointPolicy.Defeated,
            raider.Combatant.ZeroHitPointPolicy);
        Assert.Equal(13, raider.CombatProfile.ArmorClass);
        Assert.Equal(
            30,
            raider.TurnResources.MovementSpeedFeet);
        AssertRaiderSavingThrowBonuses(
            raider.CombatProfile.SavingThrowBonuses);
        Assert.Empty(
            raider.CombatProfile.DamageResponses);
        Assert.Equal(
            WeaponAttackKind.Melee,
            weapon.AttackKind);
        Assert.Equal(
            "weapon.watchtower-raider.scimitar",
            weapon.WeaponId);
        Assert.Equal(Ability.Dexterity, weapon.AttackAbility);
        Assert.Equal(2, weapon.AbilityModifier);
        Assert.True(weapon.IsProficient);
        Assert.Equal(2, weapon.ProficiencyBonus);
        Assert.Equal(4, weapon.AttackBonus);
        Assert.Equal(1, weapon.Damage.Count);
        Assert.Equal(DieType.D6, weapon.Damage.Die);
        Assert.Equal(2, weapon.DamageBonus);
        Assert.Equal("damage.slashing", weapon.DamageType);
        Assert.Equal(5, weapon.ReachFeet);
        Assert.Null(weapon.NormalRangeFeet);
        Assert.Null(weapon.LongRangeFeet);
        Assert.Null(weapon.AmmunitionItemId);
        Assert.Null(weapon.AmmunitionQuantityAvailable);
    }

    [Fact]
    public void WatchtowerEncounter_CreatesFixedRangedRaiderProfile()
    {
        EncounterParticipantState raider = FindParticipant(
            GetEncounter(
                WatchtowerSignalTestData
                    .CreateEncounterSession()),
            "combatant.watchtower-raider.ranged");
        WeaponAttack weapon = Assert.Single(
            raider.CombatProfile.WeaponAttacks);

        Assert.Equal(
            "combatant.watchtower-raider.ranged",
            raider.Combatant.CombatantId);
        Assert.Equal(
            "side.watchtower-raiders",
            raider.SideId);
        Assert.Equal(
            8,
            raider.Combatant.Health.HitPoints
                .MaximumHitPoints);
        Assert.Equal(
            8,
            raider.Combatant.Health.HitPoints
                .CurrentHitPoints);
        Assert.Equal(
            CombatantZeroHitPointPolicy.Defeated,
            raider.Combatant.ZeroHitPointPolicy);
        Assert.Equal(13, raider.CombatProfile.ArmorClass);
        Assert.Equal(
            30,
            raider.TurnResources.MovementSpeedFeet);
        AssertRaiderSavingThrowBonuses(
            raider.CombatProfile.SavingThrowBonuses);
        Assert.Empty(
            raider.CombatProfile.DamageResponses);
        Assert.Equal(
            WeaponAttackKind.Ranged,
            weapon.AttackKind);
        Assert.Equal(
            "weapon.watchtower-raider.shortbow",
            weapon.WeaponId);
        Assert.Equal(Ability.Dexterity, weapon.AttackAbility);
        Assert.Equal(2, weapon.AbilityModifier);
        Assert.True(weapon.IsProficient);
        Assert.Equal(2, weapon.ProficiencyBonus);
        Assert.Equal(4, weapon.AttackBonus);
        Assert.Equal(1, weapon.Damage.Count);
        Assert.Equal(DieType.D6, weapon.Damage.Die);
        Assert.Equal(2, weapon.DamageBonus);
        Assert.Equal("damage.piercing", weapon.DamageType);
        Assert.Null(weapon.ReachFeet);
        Assert.Equal(80, weapon.NormalRangeFeet);
        Assert.Equal(320, weapon.LongRangeFeet);
        Assert.Equal("item.arrow", weapon.AmmunitionItemId);
        Assert.Equal(12, weapon.AmmunitionQuantityAvailable);
    }

    [Fact]
    public void WatchtowerEncounter_MeleeRaiderHasInitialLegalTarget()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = AdvanceToCombatant(
            GetEncounter(state),
            "combatant.watchtower-raider.melee");

        WatchtowerCombatAttackAvailability evaluation =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                encounter,
                "combatant.watchtower-raider.melee",
                state.Party.Members[0].PartyMemberId,
                "weapon.watchtower-raider.scimitar");

        Assert.True(evaluation.IsLegal);
    }

    [Fact]
    public void WatchtowerEncounter_RangedRaiderHasInitialLegalTarget()
    {
        ApplicationSessionState state =
            WatchtowerSignalTestData.CreateEncounterSession();
        EncounterState encounter = AdvanceToCombatant(
            GetEncounter(state),
            "combatant.watchtower-raider.ranged");

        WatchtowerCombatAttackAvailability evaluation =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                encounter,
                "combatant.watchtower-raider.ranged",
                state.Party.Members[1].PartyMemberId,
                "weapon.watchtower-raider.shortbow");

        Assert.True(evaluation.IsLegal);
    }

    [Fact]
    public void WatchtowerEncounter_SameRandomStateProducesSameInitiative()
    {
        ApplicationSessionState first =
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData
                    .CreateSignalReadySession(),
                WatchtowerSignalTestData.CreateRuleset());
        ApplicationSessionState second =
            SignalMechanismRules.Activate(
                WatchtowerSignalTestData
                    .CreateSignalReadySession(),
                WatchtowerSignalTestData.CreateRuleset());
        EncounterState firstEncounter = GetEncounter(first);
        EncounterState secondEncounter = GetEncounter(second);

        Assert.Equal(
            first.RandomValuesConsumed,
            second.RandomValuesConsumed);
        Assert.Equal(
            firstEncounter.InitiativeOrder.ToArray(),
            secondEncounter.InitiativeOrder.ToArray());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(12)]
    public void WatchtowerEncounter_UsesExpectedRandomSequencePosition(
        int randomValuesConsumed)
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateSignalReadySession()
                with
                {
                    RandomValuesConsumed =
                        randomValuesConsumed
                };
        ValidatedRuleset ruleset =
            WatchtowerSignalTestData.CreateRuleset();
        ApplicationSessionState result =
            SignalMechanismRules.Activate(
                source,
                ruleset);
        EncounterState encounter = GetEncounter(result);
        int[] expectedRolls = Enumerable.Range(0, 5)
            .Select(index => GenerateExpectedDie(
                source.RandomSeed,
                randomValuesConsumed + index,
                sides: 20))
            .ToArray();
        CharacterResolver resolver = new(ruleset);
        int[] expectedBonuses = source.Party.Members
            .Select(member => resolver.Resolve(
                WatchtowerSignalTestData
                    .CreateExpectedDraft(member))
                .InitiativeBonus)
            .Concat([2, 2])
            .ToArray();
        string[] inputOrder = source.Party.Members
            .Select(member => member.PartyMemberId)
            .Concat(
            [
                "combatant.watchtower-raider.melee",
                "combatant.watchtower-raider.ranged"
            ])
            .ToArray();
        string[] expectedOrder =
            randomValuesConsumed switch
            {
                0 =>
                [
                    "combatant.watchtower-raider.ranged",
                    source.Party.Members[2].PartyMemberId,
                    source.Party.Members[0].PartyMemberId,
                    source.Party.Members[1].PartyMemberId,
                    "combatant.watchtower-raider.melee"
                ],
                12 =>
                [
                    source.Party.Members[0].PartyMemberId,
                    source.Party.Members[1].PartyMemberId,
                    "combatant.watchtower-raider.melee",
                    source.Party.Members[2].PartyMemberId,
                    "combatant.watchtower-raider.ranged"
                ],
                _ => throw new InvalidOperationException(
                    "The test random-sequence position is unsupported.")
            };

        for (int index = 0;
            index < inputOrder.Length;
            index++)
        {
            InitiativeOrderEntry actual =
                encounter.InitiativeOrder.Single(
                    entry => string.Equals(
                        entry.CombatantId,
                        inputOrder[index],
                        StringComparison.Ordinal));

            Assert.Equal(
                expectedRolls[index],
                actual.Initiative.FirstRoll);
            Assert.Equal(
                expectedBonuses[index],
                actual.Initiative.InitiativeBonus);
            Assert.Equal(
                expectedRolls[index]
                    + expectedBonuses[index],
                actual.Initiative.Total);
        }

        Assert.Equal(
            expectedOrder,
            encounter.InitiativeOrder
                .Select(entry => entry.CombatantId)
                .ToArray());

        if (randomValuesConsumed == 12)
        {
            InitiativeOrderEntry ranger =
                encounter.InitiativeOrder.Single(
                    entry => string.Equals(
                        entry.CombatantId,
                        source.Party.Members[2]
                            .PartyMemberId,
                        StringComparison.Ordinal));
            InitiativeOrderEntry rangedRaider =
                encounter.InitiativeOrder.Single(
                    entry => string.Equals(
                        entry.CombatantId,
                        "combatant.watchtower-raider.ranged",
                        StringComparison.Ordinal));

            Assert.Equal(7, ranger.Initiative.Total);
            Assert.Equal(
                ranger.Initiative.Total,
                rangedRaider.Initiative.Total);
            Assert.True(ranger.HasTiedInitiative);
            Assert.True(rangedRaider.HasTiedInitiative);
            Assert.True(
                ranger.Position
                    < rangedRaider.Position);
        }
    }

    [Fact]
    public void WatchtowerEncounter_ConsumesExactlyOneInitiativeValuePerParticipant()
    {
        ApplicationSessionState source =
            WatchtowerSignalTestData.CreateSignalReadySession();
        ApplicationSessionState result =
            SignalMechanismRules.Activate(
                source,
                WatchtowerSignalTestData.CreateRuleset());

        Assert.Equal(
            GetEncounter(result).Participants.Count,
            result.RandomValuesConsumed
                - source.RandomValuesConsumed);
        Assert.Equal(
            source.RandomSeed,
            result.RandomSeed);
    }

    private static EncounterState GetEncounter(
        ApplicationSessionState state)
    {
        return Assert.IsType<ActiveEncounterState>(
            state.ActiveEncounter).Encounter;
    }

    private static EncounterParticipantState FindParticipant(
        EncounterState state,
        string combatantId)
    {
        return state.Participants.Single(
            participant => string.Equals(
                participant.Combatant.CombatantId,
                combatantId,
                StringComparison.Ordinal));
    }

    private static void AssertWeaponAttacksEquivalent(
        IReadOnlyList<WeaponAttack> expected,
        IReadOnlyList<WeaponAttack> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (int index = 0;
            index < expected.Count;
            index++)
        {
            AssertWeaponAttackEquivalent(
                expected[index],
                actual[index]);
        }
    }

    private static void AssertWeaponAttackEquivalent(
        WeaponAttack expected,
        WeaponAttack actual)
    {
        Assert.Equal(expected.WeaponId, actual.WeaponId);
        Assert.Equal(expected.WeaponName, actual.WeaponName);
        Assert.Equal(expected.Category, actual.Category);
        Assert.Equal(expected.AttackKind, actual.AttackKind);
        Assert.Equal(expected.AttackAbility, actual.AttackAbility);
        Assert.Equal(
            expected.AbilityModifier,
            actual.AbilityModifier);
        Assert.Equal(expected.IsProficient, actual.IsProficient);
        Assert.Equal(
            expected.ProficiencyBonus,
            actual.ProficiencyBonus);
        Assert.Equal(expected.AttackBonus, actual.AttackBonus);
        Assert.Equal(
            expected.HasDisadvantage,
            actual.HasDisadvantage);
        Assert.Equal(
            expected.DisadvantageReasons.ToArray(),
            actual.DisadvantageReasons.ToArray());
        Assert.Equal(expected.AttackRollMode, actual.AttackRollMode);
        Assert.Equal(expected.Damage, actual.Damage);
        Assert.Equal(
            expected.VersatileDamage,
            actual.VersatileDamage);
        Assert.Equal(expected.DamageType, actual.DamageType);
        Assert.Equal(expected.DamageBonus, actual.DamageBonus);
        Assert.Equal(
            expected.Properties.ToArray(),
            actual.Properties.ToArray());
        Assert.Equal(expected.ReachFeet, actual.ReachFeet);
        Assert.Equal(
            expected.NormalRangeFeet,
            actual.NormalRangeFeet);
        Assert.Equal(expected.LongRangeFeet, actual.LongRangeFeet);
        Assert.Equal(
            expected.AmmunitionItemId,
            actual.AmmunitionItemId);
        Assert.Equal(
            expected.AmmunitionQuantityAvailable,
            actual.AmmunitionQuantityAvailable);
    }

    private static void AssertRaiderSavingThrowBonuses(
        IReadOnlyList<SavingThrowBonus> bonuses)
    {
        Assert.Equal(6, bonuses.Count);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Strength,
            expectedTotal: 1);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Dexterity,
            expectedTotal: 2);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Constitution,
            expectedTotal: 1);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Intelligence,
            expectedTotal: 0);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Wisdom,
            expectedTotal: 0);
        AssertSavingThrowBonus(
            bonuses,
            Ability.Charisma,
            expectedTotal: 0);
    }

    private static void AssertSavingThrowBonus(
        IReadOnlyList<SavingThrowBonus> bonuses,
        Ability ability,
        int expectedTotal)
    {
        SavingThrowBonus bonus = bonuses.Single(
            candidate => candidate.Ability == ability);

        Assert.Equal(expectedTotal, bonus.AbilityModifier);
        Assert.False(bonus.IsProficient);
        Assert.Equal(0, bonus.ProficiencyBonus);
        Assert.Equal(expectedTotal, bonus.TotalBonus);
    }

    private static EncounterState AdvanceToCombatant(
        EncounterState state,
        string combatantId)
    {
        EncounterState current = state;

        for (int index = 0;
            index < state.Participants.Count;
            index++)
        {
            if (string.Equals(
                current.ActiveCombatantId,
                combatantId,
                StringComparison.Ordinal))
            {
                return current;
            }

            current = EncounterTurnAdvancementRules.Resolve(
                current,
                new EncounterTurnAdvancementCommand
                {
                    ExpectedRevision = current.Revision,
                    ActorCombatantId =
                        current.ActiveCombatantId
                }).State;
        }

        throw new InvalidOperationException(
            $"Combatant '{combatantId}' did not become active.");
    }

    public enum TerminalHealthKind
    {
        FailedDeathSaves,
        InstantDeath
    }
    private static int GenerateExpectedDie(
        int seed,
        int cursor,
        int sides)
    {
        ulong modulus = (ulong)sides;
        ulong threshold =
            unchecked(0UL - modulus) % modulus;

        for (int attempt = 0; ; attempt++)
        {
            byte[] input = new byte[16];
            BinaryPrimitives.WriteInt32LittleEndian(
                input.AsSpan(0, 4),
                seed);
            BinaryPrimitives.WriteInt32LittleEndian(
                input.AsSpan(4, 4),
                cursor);
            BinaryPrimitives.WriteInt32LittleEndian(
                input.AsSpan(8, 4),
                sides);
            BinaryPrimitives.WriteInt32LittleEndian(
                input.AsSpan(12, 4),
                attempt);

            byte[] hash = SHA256.HashData(input);
            ulong sample =
                BinaryPrimitives.ReadUInt64LittleEndian(hash);

            if (sample >= threshold)
            {
                return checked((int)(sample % modulus) + 1);
            }
        }
    }

}
