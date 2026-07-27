using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Tests;

/// The Sunken Chapel's guardian fight, driven for real.
///
/// Until now nothing decided a guardian's turn: enemy tactics were locked to
/// the Watchtower raiders by combatant identity, so a second scenario's fight
/// could be started but not played. This drives it end to end through
/// `CombatOperations` — the same entry point Console uses — with no
/// chapel-specific code anywhere in the combat layer. What makes the
/// guardians act is the same generic policy the raiders use: a melee
/// combatant closes distance and attacks, read off its own weapon.
public sealed class SunkenChapelCombatTests
{
    [Fact]
    public void TheGuardians_ActOnTheirOwnTurnsAndThePartyCanWin()
    {
        ApplicationSessionState session =
            SecondScenarioTests.RunTo(
                SecondScenarioTests.TraversalStage.Fighting);
        bool aGuardianActed = false;

        for (int operation = 0; operation < 60; operation++)
        {
            CombatResolutionResult advanced =
                CombatOperations.AdvanceToDecision(session);

            session = advanced.State;
            aGuardianActed |= AnyGuardianActed(advanced.AutomaticSteps);

            CombatDecision decision = advanced.ResultingDecision;

            if (decision.State == CombatDecisionState.CombatCompleted)
            {
                Assert.True(
                    aGuardianActed,
                    "The guardians never acted on their own turn.");
                Assert.Equal("side.party", decision.WinningSideId);

                return;
            }

            CombatTargetOption? target = decision.WeaponAttacks
                .Where(weapon => weapon.IsAvailable)
                .SelectMany(weapon => weapon.Targets
                    .Where(candidate => candidate.IsAvailable))
                .OrderBy(candidate => candidate.DistanceFeet)
                .FirstOrDefault();

            // Automatic processing — including a guardian's own turn — runs
            // inside Execute, right after the party's action. That is where
            // the AI actually shows up, not in the next AdvanceToDecision.
            CombatResolutionResult executed = target is null
                ? CombatOperations.Execute(
                    session,
                    new CombatEndTurnIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId = decision.ActiveCombatantId!
                    })
                : CombatOperations.Execute(
                    session,
                    new CombatWeaponAttackIntent
                    {
                        ExpectedEncounterRevision =
                            decision.EncounterRevision,
                        ActorCombatantId = decision.ActiveCombatantId!,
                        WeaponId = decision.WeaponAttacks
                            .First(weapon => weapon.IsAvailable)
                            .WeaponId,
                        TargetCombatantId = target.TargetCombatantId
                    });

            session = executed.State;
            aGuardianActed |= AnyGuardianActed(executed.AutomaticSteps);

            if (executed.ResultingDecision.State
                == CombatDecisionState.CombatCompleted)
            {
                Assert.True(
                    aGuardianActed,
                    "The guardians never acted on their own turn.");
                Assert.Equal(
                    "side.party",
                    executed.ResultingDecision.WinningSideId);

                return;
            }
        }

        Assert.Fail(
            "The chapel fight did not reach a decision within the bounded operation count.");
    }

    private static bool AnyGuardianActed(
        IReadOnlyList<CombatStepResult> steps)
    {
        return steps.Any(step =>
            (step.Kind == CombatStepKind.WeaponAttack
                || step.Kind == CombatStepKind.Movement)
            && step.ActorCombatantId is not null
            && step.ActorCombatantId.StartsWith(
                "combatant.chapel-guardian.",
                StringComparison.Ordinal));
    }
}
