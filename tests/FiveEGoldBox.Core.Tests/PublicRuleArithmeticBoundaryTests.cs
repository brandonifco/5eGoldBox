using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Tests;

public sealed class PublicRuleArithmeticBoundaryTests
{
    [Fact]
    public void AttackOutcome_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            AttackRollRules.ResolveOutcome(
                naturalRoll: 2,
                attackBonus: int.MaxValue,
                targetArmorClass: 10));
    }

    [Fact]
    public void AttackResult_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            AttackRollRules.ResolveResult(
                D20RollMode.Normal,
                firstRoll: 2,
                secondRoll: null,
                attackBonus: int.MaxValue,
                targetArmorClass: 10));
    }

    [Fact]
    public void D20TestOutcome_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            D20TestRules.ResolveOutcome(
                naturalRoll: 2,
                bonus: int.MaxValue,
                difficultyClass: 10));
    }

    [Fact]
    public void D20TestResult_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            D20TestRules.ResolveResult(
                D20RollMode.Normal,
                firstRoll: 2,
                secondRoll: null,
                bonus: int.MaxValue,
                difficultyClass: 10));
    }

    [Fact]
    public void D20Contest_WhenFirstTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            D20ContestRules.ResolveContest(
                D20RollMode.Normal,
                firstRoll: 2,
                firstSecondRoll: null,
                firstBonus: int.MaxValue,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 10,
                secondSecondRoll: null,
                secondBonus: 0));
    }

    [Fact]
    public void D20Contest_WhenSecondTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            D20ContestRules.ResolveContest(
                D20RollMode.Normal,
                firstRoll: 10,
                firstSecondRoll: null,
                firstBonus: 0,
                secondRollMode: D20RollMode.Normal,
                secondRoll: 2,
                secondSecondRoll: null,
                secondBonus: int.MaxValue));
    }

    [Fact]
    public void Initiative_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            InitiativeRules.ResolveInitiative(
                D20RollMode.Normal,
                firstRoll: 2,
                secondRoll: null,
                initiativeBonus: int.MaxValue));
    }

    [Fact]
    public void DeathSavingThrow_WhenTotalOverflows_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            DeathSavingThrowRules.ResolveDeathSavingThrow(
                DeathSavingThrowRules.Create(),
                D20RollMode.Normal,
                firstRoll: 2,
                secondRoll: null,
                savingThrowBonus: int.MaxValue));
    }

    [Fact]
    public void Vulnerability_WhenDamageWouldOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            DamageRules.ApplyDamageResponse(
                int.MaxValue,
                DamageResponseType.Vulnerability));
    }

    [Fact]
    public void CombinedResponses_WhenVulnerabilityWouldOverflow_Throws()
    {
        Assert.Throws<OverflowException>(() =>
            DamageRules.ApplyDamageResponses(
                int.MaxValue,
                [DamageResponseType.Vulnerability]));
    }

    [Fact]
    public void CriticalDamageDice_WhenCountWouldOverflow_Throws()
    {
        DamageDice damage = new()
        {
            Count = int.MaxValue,
            Die = DieType.D6
        };

        Assert.Throws<OverflowException>(() =>
            DamageRules.GetCriticalHitDamageDice(damage));
    }

    [Fact]
    public void DamageRoll_WhenTotalWouldOverflow_Throws()
    {
        DamageDice damage = new()
        {
            Count = 1,
            Die = DieType.D6
        };

        Assert.Throws<OverflowException>(() =>
            DamageRules.ResolveDamageRoll(
                damage,
                rolls: [1],
                damageBonus: int.MaxValue));
    }

    [Fact]
    public void Vulnerability_AtMaximumRepresentableBoundary_RemainsValid()
    {
        int result = DamageRules.ApplyDamageResponse(
            int.MaxValue / 2,
            DamageResponseType.Vulnerability);

        Assert.Equal(int.MaxValue - 1, result);
    }

    [Fact]
    public void AttackResult_AtMaximumRepresentableBoundary_RemainsValid()
    {
        AttackRollResult result = AttackRollRules.ResolveResult(
            D20RollMode.Normal,
            firstRoll: 20,
            secondRoll: null,
            attackBonus: int.MaxValue - 20,
            targetArmorClass: int.MaxValue);

        Assert.Equal(int.MaxValue, result.Total);
        Assert.Equal(AttackRollOutcome.CriticalHit, result.Outcome);
    }

    [Fact]
    public void ApplyHealing_WithExtremeAmount_CapsWithoutIntermediateOverflow()
    {
        HitPointState state = new()
        {
            MaximumHitPoints = int.MaxValue,
            CurrentHitPoints = int.MaxValue - 1,
            TemporaryHitPoints = 0
        };

        HitPointState result = HitPointRules.ApplyHealing(
            state,
            int.MaxValue);

        Assert.Equal(int.MaxValue, result.CurrentHitPoints);
    }

    [Fact]
    public void AdvanceTurn_WhenRoundNumberWouldOverflow_Throws()
    {
        CombatTurnState state = new()
        {
            InitiativeOrder =
            [
                new InitiativeOrderEntry
                {
                    CombatantId = "combatant.test",
                    Initiative = new InitiativeRollResult
                    {
                        RollMode = D20RollMode.Normal,
                        FirstRoll = 10,
                        SecondRoll = null,
                        NaturalRoll = 10,
                        InitiativeBonus = 0,
                        Total = 10
                    },
                    Position = 1,
                    HasTiedInitiative = false
                }
            ],
            RoundNumber = int.MaxValue,
            ActivePosition = 1
        };

        Assert.Throws<OverflowException>(() =>
            CombatTurnRules.AdvanceTurn(state));
    }
}
