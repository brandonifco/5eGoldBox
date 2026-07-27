using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Tests;

/// Direct tests for the raider turn planner extracted from
/// WatchtowerCombatOrchestrator. Planning is a pure function over encounter
/// state, so these exercise its decision branches without driving a whole
/// combat. The melee close-then-attack paths are covered end to end by the
/// orchestrator characterization transcripts.
public sealed class EncounterTacticsTurnPlannerTests
{
    private const string RangedRaiderId =
        "combatant.watchtower-raider.ranged";

    private const string MeleeRaiderId =
        "combatant.watchtower-raider.melee";

    [Fact]
    public void Plan_RaiderWithinRange_PlansAttackWithoutMovement()
    {
        ApplicationSessionState state = ActivateRaider(RangedRaiderId);
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(state);

        EncounterTacticsTurnPlan plan =
            EncounterTacticsTurnPlanner.Plan(encounter, state.Party);

        Assert.Null(plan.Movement);
        EncounterTacticsAttackPlan attack =
            Assert.IsType<EncounterTacticsAttackPlan>(plan.Attack);
        Assert.Equal(
            GetFixedWeaponId(state, RangedRaiderId),
            attack.WeaponId);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.RaiderTurnCompleted,
            plan.TurnAdvanceReason);
    }

    [Fact]
    public void Plan_RaiderWithoutAmmunition_PlansNothingProductive()
    {
        ApplicationSessionState state = ReplaceRaiderWeapon(
            ActivateRaider(RangedRaiderId),
            RangedRaiderId,
            weapon => weapon with { AmmunitionQuantityAvailable = 0 });

        EncounterTacticsTurnPlan plan = EncounterTacticsTurnPlanner.Plan(
            WatchtowerCombatTestData.GetEncounter(state),
            state.Party);

        Assert.Null(plan.Movement);
        Assert.Null(plan.Attack);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction,
            plan.TurnAdvanceReason);
    }

    /// Only the melee raider repositions. A ranged raider that cannot legally
    /// attack ends its turn where it stands.
    [Fact]
    public void Plan_RangedRaiderWithIllegalAttack_PlansNoMovement()
    {
        ApplicationSessionState state = ReplaceRaiderWeapon(
            ActivateRaider(RangedRaiderId),
            RangedRaiderId,
            weapon => weapon with
            {
                NormalRangeFeet = 5,
                LongRangeFeet = 5
            });

        EncounterTacticsTurnPlan plan = EncounterTacticsTurnPlanner.Plan(
            WatchtowerCombatTestData.GetEncounter(state),
            state.Party);

        Assert.Null(plan.Movement);
        Assert.Null(plan.Attack);
        Assert.Equal(
            WatchtowerCombatTurnAdvanceReason.NoProductiveEnemyAction,
            plan.TurnAdvanceReason);
    }

    [Theory]
    [InlineData(RangedRaiderId)]
    [InlineData(MeleeRaiderId)]
    public void Plan_IsRepeatableAndLeavesTheEncounterUntouched(
        string raiderId)
    {
        ApplicationSessionState state = ActivateRaider(raiderId);
        EncounterState encounter =
            WatchtowerCombatTestData.GetEncounter(state);
        long revisionBefore = encounter.Revision;
        GridPosition[] positionsBefore = encounter.Participants
            .Select(participant => participant.Position)
            .ToArray();

        EncounterTacticsTurnPlan first =
            EncounterTacticsTurnPlanner.Plan(encounter, state.Party);
        EncounterTacticsTurnPlan second =
            EncounterTacticsTurnPlanner.Plan(encounter, state.Party);

        Assert.Equal(first.Attack, second.Attack);
        Assert.Equal(first.TurnAdvanceReason, second.TurnAdvanceReason);
        Assert.Equal(
            first.Movement?.EndingPosition,
            second.Movement?.EndingPosition);

        Assert.Equal(revisionBefore, encounter.Revision);
        Assert.Equal(
            positionsBefore,
            encounter.Participants
                .Select(participant => participant.Position)
                .ToArray());
        Assert.Equal(
            revisionBefore,
            WatchtowerCombatTestData.GetEncounter(state).Revision);
    }

    private static ApplicationSessionState ActivateRaider(
        string raiderId)
    {
        return WatchtowerCombatTestData.AdvanceToCombatant(
            WatchtowerSignalTestData.CreateEncounterSession(),
            raiderId);
    }

    private static string GetFixedWeaponId(
        ApplicationSessionState state,
        string combatantId)
    {
        return Assert.Single(
            WatchtowerCombatTestData
                .GetParticipant(state, combatantId)
                .CombatProfile.WeaponAttacks)
            .WeaponId;
    }

    private static ApplicationSessionState ReplaceRaiderWeapon(
        ApplicationSessionState state,
        string raiderId,
        Func<WeaponAttack, WeaponAttack> changeWeapon)
    {
        EncounterParticipantState raider =
            WatchtowerCombatTestData.GetParticipant(state, raiderId);
        WeaponAttack weapon = Assert.Single(
            raider.CombatProfile.WeaponAttacks);

        return WatchtowerCombatTestData.ReplaceParticipant(
            state,
            raider with
            {
                CombatProfile = raider.CombatProfile with
                {
                    WeaponAttacks = [changeWeapon(weapon)]
                }
            });
    }
}
