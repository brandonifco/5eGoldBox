using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

internal static class EncounterCombatRules
{
    public static EncounterCombatResolutionResult AdvanceToDecision(
        ApplicationSessionState session)
    {
        return EncounterCombatOrchestrator.AdvanceToDecision(session);
    }

    public static EncounterCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatMoveIntent intent)
    {
        return EncounterCombatOrchestrator.Execute(session, intent);
    }

    public static EncounterCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatWeaponAttackIntent intent)
    {
        return EncounterCombatOrchestrator.Execute(session, intent);
    }

    public static EncounterCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatSpellAttackIntent intent)
    {
        return EncounterCombatOrchestrator.Execute(session, intent);
    }

    public static EncounterCombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatEndTurnIntent intent)
    {
        return EncounterCombatOrchestrator.Execute(session, intent);
    }
}
