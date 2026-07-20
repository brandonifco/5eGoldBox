using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

public static class WatchtowerCombatRules
{
    public static WatchtowerCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState session)
    {
        return WatchtowerCombatOrchestrator.AdvanceToDecision(session);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        WatchtowerCombatMoveIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        WatchtowerCombatWeaponAttackIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        WatchtowerCombatEndTurnIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }
}
