using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

public sealed class EncounterCombatAttackStagingTests
{
    [Fact]
    public void EvaluateAvailability_ProjectsKnownAdjacentRangedAttackFacts()
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

        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        WeaponAttack weapon = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        EncounterCombatAttackAvailability availability =
            EncounterCombatAttackStaging.EvaluateAvailability(
                encounter,
                archer.Combatant.CombatantId,
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
            EncounterCombatTestData.CreatePlayerDecisionSession();
        EncounterState sourceEncounter =
            EncounterCombatTestData.GetEncounter(source);
        EncounterParticipantState actor =
            EncounterCombatTestData.GetParticipant(
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
        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            actor);

        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        EncounterCombatDecision decision =
            EncounterCombatRules.AdvanceToDecision(source)
                .ResultingDecision;
        EncounterCombatWeaponAttackOption attackOption =
            decision.WeaponAttacks.Single();
        string actorId = Assert.IsType<string>(
            decision.ActiveCombatantId);
        EncounterCombatTargetOption target =
            attackOption.Targets.First(
                candidate => candidate.IsAvailable);
        int cursorBefore = source.RandomValuesConsumed;

        EncounterCombatAttackExecution execution =
            EncounterCombatAttackStaging.Resolve(
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
                    == CombatDiePurpose.AttackRoll));
        Assert.Equal(
            execution.Result.Attack.AttackRoll.Outcome
                == AttackRollOutcome.Miss
                    ? 0
                    : execution.Result.Attack.Damage
                        .DamageDice!.Count,
            execution.Dice.Count(die =>
                die.Purpose
                    == CombatDiePurpose.DamageRoll));
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
                CombatDiePurpose.AttackRoll,
                die.Purpose));
        Assert.All(
            execution.Dice.Skip(2),
            die => Assert.Equal(
                CombatDiePurpose.DamageRoll,
                die.Purpose));

        Assert.Equal(
            encounter.Revision,
            EncounterCombatTestData.GetEncounter(source).Revision);
        Assert.True(
            EncounterCombatTestData.GetParticipant(
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
            EncounterCombatTestData.AdvanceToCombatant(
                EncounterCombatTestData.CreatePlayerDecisionSession(),
                "party-member.rogue");
        EncounterParticipantState archer =
            EncounterCombatTestData.GetParticipant(
                source,
                "party-member.rogue");
        WeaponAttack weapon = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        ApplicationSessionState priorSource =
            EncounterCombatTestData.ReplaceParticipant(
                source,
                archer with
                {
                    Position = new GridPosition(2, 2)
                });
        EncounterCombatAttackAvailability priorAvailability =
            EncounterCombatAttackStaging.EvaluateAvailability(
                EncounterCombatTestData.GetEncounter(priorSource),
                archer.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.True(priorAvailability.IsLegal);
        Assert.Equal(
            D20RollMode.Disadvantage,
            priorAvailability.AttackRollMode);

        ApplicationSessionState currentSource =
            EncounterCombatTestData.ReplaceParticipant(
                source,
                archer with
                {
                    Position = new GridPosition(0, 3)
                });
        EncounterState currentEncounter =
            EncounterCombatTestData.GetEncounter(currentSource);
        int cursorBefore = currentSource.RandomValuesConsumed;

        EncounterCombatAttackExecution execution =
            EncounterCombatAttackStaging.Resolve(
                currentEncounter,
                currentSource.RandomSeed,
                cursorBefore,
                archer.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.Equal(
            D20RollMode.Normal,
            execution.Result.Attack.AttackRoll.RollMode);
        Assert.Single(
            execution.Dice,
            die => die.Purpose
                == CombatDiePurpose.AttackRoll);
        Assert.Equal(
            execution.Dice.Count,
            execution.CursorAfter - cursorBefore);
        Assert.Equal(
            CombatDiePurpose.AttackRoll,
            execution.Dice[0].Purpose);
        Assert.All(
            execution.Dice.Skip(1),
            die => Assert.Equal(
                CombatDiePurpose.DamageRoll,
                die.Purpose));
    }

    /// A contribution that adds dice changes how many the caller owes, and the
    /// caller is the one holding the random sequence. The extra die is drawn
    /// after the d20 and before the damage, because the contribution can turn
    /// a miss into a hit and so decides whether damage is rolled at all.
    [Fact]
    public void Resolve_BlessedAttacker_DrawsTheExtraDieBetweenAttackAndDamage()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                EncounterCombatTestData.CreatePlayerDecisionSession(),
                "party-member.rogue");
        EncounterParticipantState archer =
            EncounterCombatTestData.GetParticipant(
                source,
                "party-member.rogue");
        WeaponAttack weapon = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            archer with
            {
                // Out of the melee raider's reach, so the shot is a plain one
                // and the only extra die is the blessing's.
                Position = new GridPosition(0, 3),
                ActiveEffects =
                [
                    new ActiveEffect
                    {
                        EffectId = "effect.bless",
                        SourceCombatantId = "party-member.rogue",
                        RemainingRounds = 10,
                        RequiresConcentration = true,
                        Contributions =
                        [
                            new RollContributionDefinition
                            {
                                Target =
                                    RollContributionTarget.AttackRoll,
                                Dice = new DamageDice
                                {
                                    Count = 1,
                                    Die = DieType.D4
                                }
                            }
                        ]
                    }
                ]
            });

        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        int cursorBefore = source.RandomValuesConsumed;

        EncounterCombatAttackAvailability availability =
            EncounterCombatAttackStaging.EvaluateAvailability(
                encounter,
                archer.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.Equal(
            new[] { DieType.D4 },
            availability.AttackRollContributions.RequiredDice);

        EncounterCombatAttackExecution execution =
            EncounterCombatAttackStaging.Resolve(
                encounter,
                source.RandomSeed,
                cursorBefore,
                archer.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        EncounterCombatDieRoll contribution = execution.Dice[1];

        Assert.Equal(
            D20RollMode.Normal,
            execution.Result.Attack.AttackRoll.RollMode);
        Assert.Equal(20, execution.Dice[0].Sides);
        Assert.Equal(4, contribution.Sides);
        Assert.Equal(
            CombatDiePurpose.AttackRoll,
            contribution.Purpose);
        Assert.All(
            execution.Dice.Skip(2),
            die => Assert.Equal(
                CombatDiePurpose.DamageRoll,
                die.Purpose));

        // The contribution lands in the attack bonus, which is what adding a
        // d4 to an attack roll means.
        Assert.Equal(
            weapon.AttackBonus + contribution.Value,
            execution.Result.Attack.AttackRoll.AttackBonus);
        Assert.Equal(
            execution.Dice.Count,
            execution.CursorAfter - cursorBefore);
        Assert.Equal(
            Enumerable.Range(
                cursorBefore + 1,
                execution.Dice.Count),
            execution.Dice.Select(die => die.Ordinal));
    }

    /// The other half of the seam, reached from a feature rather than a spell:
    /// extra damage declared on the combat profile, gated on a condition the
    /// attack has to answer. The dice come last, since nothing downstream
    /// depends on them.
    [Fact]
    public void Resolve_ExtraDamageFromAFeature_DrawsItsDiceAfterTheWeaponsOwn()
    {
        ApplicationSessionState source =
            EncounterCombatTestData.AdvanceToCombatant(
                EncounterCombatTestData.CreatePlayerDecisionSession(),
                "party-member.rogue");
        EncounterParticipantState archer =
            EncounterCombatTestData.GetParticipant(
                source,
                "party-member.rogue");
        WeaponAttack weapon = Assert.Single(
            archer.CombatProfile.WeaponAttacks,
            candidate => candidate.WeaponId == "weapon.shortbow");

        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            archer with
            {
                // Out of the melee raider's reach, so the shot is a plain one
                // and the only extra die is the blessing's.
                Position = new GridPosition(0, 3),
                CombatProfile = archer.CombatProfile with
                {
                    Contributions =
                    [
                        new RollContributionDefinition
                        {
                            Target = RollContributionTarget.DamageRoll,
                            Dice = new DamageDice
                            {
                                Count = 1,
                                Die = DieType.D6
                            },
                            Conditions =
                            [
                                RollContributionCondition
                                    .FinesseOrRangedWeapon
                            ]
                        }
                    ]
                }
            });

        // The contribution only shows up on a hit, so the raider is made easy
        // to hit rather than the test hoping this seed obliges.
        EncounterParticipantState raider =
            EncounterCombatTestData.GetParticipant(
                source,
                "combatant.watchtower-raider.melee");
        source = EncounterCombatTestData.ReplaceParticipant(
            source,
            raider with
            {
                CombatProfile = raider.CombatProfile with
                {
                    ArmorClass = 5
                }
            });

        EncounterState encounter =
            EncounterCombatTestData.GetEncounter(source);
        int cursorBefore = source.RandomValuesConsumed;

        EncounterCombatAttackExecution execution =
            EncounterCombatAttackStaging.Resolve(
                encounter,
                source.RandomSeed,
                cursorBefore,
                archer.Combatant.CombatantId,
                "combatant.watchtower-raider.melee",
                weapon.WeaponId);

        Assert.NotEqual(
            AttackRollOutcome.Miss,
            execution.Result.Attack.AttackRoll.Outcome);

        EncounterCombatDieRoll[] damageDice = execution.Dice
            .Where(die => die.Purpose
                == CombatDiePurpose.DamageRoll)
            .ToArray();
        EncounterCombatDieRoll contribution = damageDice[^1];

        // Last of everything drawn for this attack.
        Assert.Equal(
            execution.Dice.Count,
            contribution.Ordinal - cursorBefore);
        Assert.Equal(6, contribution.Sides);
        Assert.Equal(
            execution.Result.Attack.Damage.DamageDice!.Count + 1,
            damageDice.Length);

        // A longbow is ranged, so the condition holds and the die counts.
        Assert.Equal(
            weapon.DamageBonus + contribution.Value,
            execution.Result.Attack.Damage.DamageRoll!.DamageBonus);
        Assert.Equal(
            execution.Dice.Count,
            execution.CursorAfter - cursorBefore);
        Assert.Equal(
            Enumerable.Range(
                cursorBefore + 1,
                execution.Dice.Count),
            execution.Dice.Select(die => die.Ordinal));
    }
}
