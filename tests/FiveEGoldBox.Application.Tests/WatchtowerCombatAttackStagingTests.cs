using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class WatchtowerCombatAttackStagingTests
{
    [Fact]
    public void EvaluateAvailability_ProjectsKnownAdjacentRangedAttackFacts()
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

        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);
        WeaponAttack weapon = Assert.Single(
            ranger.CombatProfile.WeaponAttacks);

        WatchtowerCombatAttackAvailability availability =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                encounter,
                ranger.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.True(availability.IsLegal);
        Assert.Equal(
            EncounterActionUnavailabilityReason.None,
            availability.UnavailabilityReason);
        Assert.Equal(
            D20RollMode.Disadvantage,
            availability.AttackRollMode);
        Assert.Equal(5, availability.DistanceFeet);
    }

    [Fact]
    public void Resolve_AdvantageAttack_ConsumesOrderedLogicalDiceWithoutMutatingSource()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.CreatePlayerDecisionSession();
        EncounterState sourceEncounter =
            WatchtowerCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            WatchtowerCombatTestData.GetParticipant(
                source,
                sourceEncounter.ActiveCombatantId);
        WeaponAttack originalWeapon = Assert.Single(
            actor.CombatProfile.WeaponAttacks);
        WeaponAttack weapon = originalWeapon with
        {
            AttackRollMode = D20RollMode.Advantage
        };

        actor = actor with
        {
            CombatProfile = actor.CombatProfile with
            {
                WeaponAttacks = Array.AsReadOnly(new[] { weapon })
            }
        };
        source = WatchtowerCombatTestData.ReplaceParticipant(
            source,
            actor);

        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(source);
        WatchtowerCombatDecision decision =
            WatchtowerCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        WatchtowerCombatWeaponAttackOption attackOption =
            Assert.IsType<WatchtowerCombatWeaponAttackOption>(
                decision.WeaponAttack);
        string actorId = Assert.IsType<string>(
            decision.ActiveCombatantId);
        WatchtowerCombatTargetOption target =
            attackOption.Targets.First(
                candidate => candidate.IsAvailable);
        int cursorBefore = source.RandomValuesConsumed;

        WatchtowerCombatAttackExecution execution =
            WatchtowerCombatAttackStaging.Resolve(
                encounter,
                source.RandomSeed,
                cursorBefore,
                actorId,
                target.TargetCombatantId,
                attackOption.WeaponId);

        Assert.Equal(
            D20RollMode.Advantage,
            execution.Result.Attack.AttackRoll.RollMode);
        Assert.Equal(
            2,
            execution.Dice.Count(die =>
                die.Purpose
                    == WatchtowerCombatDiePurpose.AttackRoll));
        Assert.Equal(
            execution.Result.Attack.AttackRoll.Outcome
                == AttackRollOutcome.Miss
                    ? 0
                    : execution.Result.Attack.Damage
                        .DamageDice!.Count,
            execution.Dice.Count(die =>
                die.Purpose
                    == WatchtowerCombatDiePurpose.DamageRoll));
        Assert.Equal(
            execution.Dice.Count,
            execution.CursorAfter - cursorBefore);
        Assert.Equal(
            Enumerable.Range(
                cursorBefore + 1,
                execution.Dice.Count),
            execution.Dice.Select(die => die.Ordinal));
        Assert.All(
            execution.Dice.Take(2),
            die => Assert.Equal(
                WatchtowerCombatDiePurpose.AttackRoll,
                die.Purpose));
        Assert.All(
            execution.Dice.Skip(2),
            die => Assert.Equal(
                WatchtowerCombatDiePurpose.DamageRoll,
                die.Purpose));

        Assert.Equal(
            encounter.Revision,
            WatchtowerCombatTestData.GetEncounter(source).Revision);
        Assert.True(
            WatchtowerCombatTestData.GetParticipant(
                source,
                actorId)
            .TurnResources.HasActionAvailable);
        Assert.False(
            execution.Result.State.Participants.Single(
                participant => string.Equals(
                    participant.Combatant.CombatantId,
                    actorId,
                    StringComparison.Ordinal))
            .TurnResources.HasActionAvailable);
    }

    [Fact]
    public void Resolve_RecomputesCurrentNormalModeInsteadOfUsingPriorDisadvantageProjection()
    {
        ApplicationSessionState source =
            WatchtowerCombatTestData.AdvanceToCombatant(
                WatchtowerCombatTestData.CreatePlayerDecisionSession(),
                "party-member.ranger");
        EncounterParticipantState ranger =
            WatchtowerCombatTestData.GetParticipant(
                source,
                "party-member.ranger");
        WeaponAttack weapon = Assert.Single(
            ranger.CombatProfile.WeaponAttacks);

        ApplicationSessionState priorSource =
            WatchtowerCombatTestData.ReplaceParticipant(
                source,
                ranger with
                {
                    Position = new GridPosition(2, 2)
                });
        WatchtowerCombatAttackAvailability priorAvailability =
            WatchtowerCombatAttackStaging.EvaluateAvailability(
                WatchtowerCombatTestData.GetEncounter(priorSource),
                ranger.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.True(priorAvailability.IsLegal);
        Assert.Equal(
            D20RollMode.Disadvantage,
            priorAvailability.AttackRollMode);

        ApplicationSessionState currentSource =
            WatchtowerCombatTestData.ReplaceParticipant(
                source,
                ranger with
                {
                    Position = new GridPosition(0, 3)
                });
        EncounterState currentEncounter =
            WatchtowerCombatTestData.GetEncounter(currentSource);
        int cursorBefore = currentSource.RandomValuesConsumed;

        WatchtowerCombatAttackExecution execution =
            WatchtowerCombatAttackStaging.Resolve(
                currentEncounter,
                currentSource.RandomSeed,
                cursorBefore,
                ranger.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.Equal(
            D20RollMode.Normal,
            execution.Result.Attack.AttackRoll.RollMode);
        Assert.Single(
            execution.Dice,
            die => die.Purpose
                == WatchtowerCombatDiePurpose.AttackRoll);
        Assert.Equal(
            execution.Dice.Count,
            execution.CursorAfter - cursorBefore);
        Assert.Equal(
            WatchtowerCombatDiePurpose.AttackRoll,
            execution.Dice[0].Purpose);
        Assert.All(
            execution.Dice.Skip(1),
            die => Assert.Equal(
                WatchtowerCombatDiePurpose.DamageRoll,
                die.Purpose));
    }
}
