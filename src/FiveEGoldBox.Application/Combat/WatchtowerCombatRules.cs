using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

internal static class WatchtowerCombatRules
{
    public static WatchtowerCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState session)
    {
        return WatchtowerCombatOrchestrator.AdvanceToDecision(session);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatMoveIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatWeaponAttackIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatSpellAttackIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }

    public static WatchtowerCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatEndTurnIntent intent)
    {
        return WatchtowerCombatOrchestrator.Execute(session, intent);
    }
}
