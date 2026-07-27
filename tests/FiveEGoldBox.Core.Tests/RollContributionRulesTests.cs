using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Core.Tests;

/// Asking a combatant what its effects contribute to a roll, and totalling
/// what came back. The two halves are deliberately separate: the first says
/// what to roll, the second consumes what was rolled.
public sealed class RollContributionRulesTests
{
    [Fact]
    public void Resolve_ACombatantNothingIsAffecting_ContributesNothing()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(),
            RollContributionTarget.AttackRoll);

        Assert.True(contributions.IsEmpty);
        Assert.Equal(0, contributions.FlatBonus);
        Assert.Empty(contributions.RequiredDice);
        Assert.Equal(
            RollContributionTarget.AttackRoll,
            contributions.Target);
    }

    [Fact]
    public void Resolve_AnEffectAddingADie_AsksTheCallerToRollIt()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4))),
            RollContributionTarget.AttackRoll);

        Assert.False(contributions.IsEmpty);
        Assert.Equal(0, contributions.FlatBonus);
        Assert.Equal([DieType.D4], contributions.RequiredDice);
    }

    /// A flat bonus changes the roll without changing how many dice it takes,
    /// which is the case a caller must not have to special-case.
    [Fact]
    public void Resolve_AnEffectAddingAFlatBonus_AsksForNoDice()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.archery",
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.AttackRoll,
                        FlatBonus = 2
                    })),
            RollContributionTarget.AttackRoll);

        Assert.False(contributions.IsEmpty);
        Assert.Equal(2, contributions.FlatBonus);
        Assert.Empty(contributions.RequiredDice);
    }

    /// Which roll a contribution names is the whole of what separates Bless's
    /// two halves, so the query has to honour it.
    [Fact]
    public void Resolve_IgnoresContributionsToAnotherRoll()
    {
        EncounterParticipantState participant = CreateParticipant(
            CreateEffect(
                "effect.bless",
                Dice(RollContributionTarget.AttackRoll, 1, DieType.D4),
                Dice(RollContributionTarget.SavingThrow, 1, DieType.D4)));

        Assert.Equal(
            [DieType.D4],
            RollContributionRules.Resolve(
                participant,
                RollContributionTarget.SavingThrow)
                .RequiredDice);
        Assert.True(
            RollContributionRules.Resolve(
                participant,
                RollContributionTarget.DamageRoll)
                .IsEmpty);
    }

    /// Two effects on one combatant both count, and a contribution of several
    /// dice is asked for one die at a time — the caller rolls a list, not a
    /// count.
    [Fact]
    public void Resolve_GathersEveryEffectAndFlattensTheirDice()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4)),
                CreateEffect(
                    "effect.inspiration",
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.AttackRoll,
                        FlatBonus = 1,
                        Dice = new DamageDice
                        {
                            Count = 2,
                            Die = DieType.D6
                        }
                    })),
            RollContributionTarget.AttackRoll);

        Assert.Equal(1, contributions.FlatBonus);
        Assert.Equal(
            [DieType.D4, DieType.D6, DieType.D6],
            contributions.RequiredDice);
    }

    /// Nothing counts rounds down yet, but an effect with none left is over
    /// and must not reach a roll.
    [Fact]
    public void Resolve_IgnoresAnEffectWithNoRoundsLeft()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4))
                    with
                {
                    RemainingRounds = 0
                }),
            RollContributionTarget.AttackRoll);

        Assert.True(contributions.IsEmpty);
    }

    [Fact]
    public void Resolve_ByCombatantId_FindsTheParticipant()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.True(
            RollContributionRules.Resolve(
                state,
                "combatant.caster",
                RollContributionTarget.AttackRoll)
                .IsEmpty);
    }

    [Fact]
    public void Resolve_ByAnUnknownCombatantId_Throws()
    {
        EncounterState state = SpellTestData.CreateEncounter();

        Assert.Throws<ArgumentException>(() =>
            RollContributionRules.Resolve(
                state,
                "combatant.nobody",
                RollContributionTarget.AttackRoll));
    }

    [Fact]
    public void Total_AddsTheFlatBonusToEveryDieRolled()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.inspiration",
                    new RollContributionDefinition
                    {
                        Target = RollContributionTarget.AttackRoll,
                        FlatBonus = 2,
                        Dice = new DamageDice
                        {
                            Count = 2,
                            Die = DieType.D6
                        }
                    })),
            RollContributionTarget.AttackRoll);

        Assert.Equal(
            9,
            RollContributionRules.Total(
                contributions,
                [3, 4]));
    }

    [Fact]
    public void Total_OfNothing_IsNothing()
    {
        Assert.Equal(
            0,
            RollContributionRules.Total(
                RollContributionSet.None(
                    RollContributionTarget.AttackRoll),
                []));
    }

    /// A caller that hands in the wrong number of dice did not ask what the
    /// roll needed. Ignoring the difference would make a blessed attack
    /// quietly unblessed, which is the failure this whole seam exists to
    /// prevent.
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Total_GivenTheWrongNumberOfDice_Throws(
        int rollCount)
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4))),
            RollContributionTarget.AttackRoll);

        Assert.Throws<ArgumentException>(() =>
            RollContributionRules.Total(
                contributions,
                Enumerable.Repeat(1, rollCount).ToArray()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Total_GivenARollTheDieCannotProduce_Throws(
        int roll)
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4))),
            RollContributionTarget.AttackRoll);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RollContributionRules.Total(
                contributions,
                [roll]));
    }

    /// A feature is not an effect and does not sit on the creature — it is
    /// part of what the creature is. The query cannot tell the difference, and
    /// that is the whole reason this seam is not spell-shaped.
    [Fact]
    public void Resolve_GathersWhatTheCombatantIsAsWellAsWhatIsOnIt()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                [Dice(RollContributionTarget.AttackRoll, 1, DieType.D8)],
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.AttackRoll, 1, DieType.D4))),
            RollContributionTarget.AttackRoll);

        Assert.Equal(
            [DieType.D8, DieType.D4],
            contributions.RequiredDice);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void Resolve_AConditionalContribution_NeedsEveryConditionToHold(
        bool hasOpening,
        bool finesseOrRanged,
        bool expectsDie)
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                [
                    Dice(RollContributionTarget.DamageRoll, 1, DieType.D6)
                        with
                    {
                        Conditions =
                        [
                            RollContributionCondition
                                .AdvantageOrAdjacentEnemy,
                            RollContributionCondition
                                .FinesseOrRangedWeapon
                        ]
                    }
                ]),
            RollContributionTarget.DamageRoll,
            new RollContributionContext
            {
                AttackRollMode = hasOpening
                    ? D20RollMode.Advantage
                    : D20RollMode.Normal,
                TargetHasAdjacentEnemy = false,
                WeaponIsFinesseOrRanged = finesseOrRanged
            });

        Assert.Equal(
            expectsDie,
            contributions.RequiredDice.Count == 1);
    }

    /// Dropping a contribution because the roll forgot to say what it looked
    /// like is the same class of bug as handing in too few dice, so it is an
    /// error rather than a silent "no".
    [Fact]
    public void Resolve_AConditionalContributionAndNoContext_Throws()
    {
        EncounterParticipantState participant = CreateParticipant(
            [
                Dice(RollContributionTarget.DamageRoll, 1, DieType.D6)
                    with
                {
                    Conditions =
                    [
                        RollContributionCondition.FinesseOrRangedWeapon
                    ]
                }
            ]);

        Assert.Throws<InvalidOperationException>(() =>
            RollContributionRules.Resolve(
                participant,
                RollContributionTarget.DamageRoll));
    }

    /// A contribution that asks nothing needs no context, which is why Bless
    /// still resolves through the saving-throw path that supplies none.
    [Fact]
    public void Resolve_AnUnconditionalContributionAndNoContext_IsFine()
    {
        RollContributionSet contributions = RollContributionRules.Resolve(
            CreateParticipant(
                CreateEffect(
                    "effect.bless",
                    Dice(RollContributionTarget.SavingThrow, 1, DieType.D4))),
            RollContributionTarget.SavingThrow);

        Assert.Equal([DieType.D4], contributions.RequiredDice);
    }

    private static RollContributionDefinition Dice(
        RollContributionTarget target,
        int count,
        DieType die)
    {
        return new RollContributionDefinition
        {
            Target = target,
            Dice = new DamageDice
            {
                Count = count,
                Die = die
            }
        };
    }

    private static ActiveEffect CreateEffect(
        string effectId,
        params RollContributionDefinition[] contributions)
    {
        return new ActiveEffect
        {
            EffectId = effectId,
            SourceCombatantId = "combatant.caster",
            RemainingRounds = 10,
            RequiresConcentration = false,
            Contributions = contributions
        };
    }

    private static EncounterParticipantState CreateParticipant(
        params ActiveEffect[] effects)
    {
        return CreateParticipant(
            Array.Empty<RollContributionDefinition>(),
            effects);
    }

    private static EncounterParticipantState CreateParticipant(
        IReadOnlyList<RollContributionDefinition> profileContributions,
        params ActiveEffect[] effects)
    {
        return new EncounterParticipantState
        {
            Combatant = CombatantRules.Create(
                "combatant.actor",
                maximumHitPoints: 10,
                CombatantZeroHitPointPolicy.DeathSavingThrows),
            CombatProfile = new EncounterCombatProfile
            {
                ArmorClass = 12,
                Contributions = profileContributions
            },
            SideId = "side.party",
            TurnResources = CombatTurnResourceRules.StartTurn(
                movementSpeedFeet: 30),
            Position = new GridPosition(1, 1),
            ActiveEffects = effects
        };
    }
}
