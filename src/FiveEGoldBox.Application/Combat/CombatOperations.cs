using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

/// The entry point for reading and driving an encounter. Query inspects the
/// current state; AdvanceToDecision and Execute move it forward.
///
/// Every operation returns the updated session, which callers must continue
/// from — sessions are immutable and nothing is mutated in place.
public static class CombatOperations
{
    public static CombatView Query(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonical =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonical.ActiveEncounter is null)
        {
            throw new InvalidOperationException(
                "The current application session does not contain an active encounter.");
        }

        HashSet<string> controlledCombatantIds =
            canonical.Party.Members
                .Select(member => member.PartyMemberId)
                .ToHashSet(StringComparer.Ordinal);

        return CombatViewFactory.Create(
            canonical.ActiveEncounter.Encounter,
            controlledCombatantIds);
    }

    /// Runs automatic processing until a player decision is required or the
    /// encounter ends. Safe to call when a decision is already pending; it then
    /// reports that decision without changing anything.
    public static CombatResolutionResult AdvanceToDecision(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return WatchtowerCombatResultMapper.ToCombatResolutionResult(
            WatchtowerCombatOrchestrator.AdvanceToDecision(session));
    }

    public static CombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatMoveIntent intent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(intent.Path);

        return WatchtowerCombatResultMapper.ToCombatResolutionResult(
            WatchtowerCombatOrchestrator.Execute(
                session,
                new WatchtowerCombatMoveIntent
                {
                    ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId,
                    Path = intent.Path
                }));
    }

    public static CombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatWeaponAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intent);

        return WatchtowerCombatResultMapper.ToCombatResolutionResult(
            WatchtowerCombatOrchestrator.Execute(
                session,
                new WatchtowerCombatWeaponAttackIntent
                {
                    ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId,
                    WeaponId = intent.WeaponId,
                    TargetCombatantId = intent.TargetCombatantId
                }));
    }

    public static CombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatSpellAttackIntent intent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intent);

        return WatchtowerCombatResultMapper.ToCombatResolutionResult(
            WatchtowerCombatOrchestrator.Execute(
                session,
                new WatchtowerCombatSpellAttackIntent
                {
                    ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId,
                    SpellId = intent.SpellId,
                    TargetCombatantId = intent.TargetCombatantId
                }));
    }

    public static CombatResolutionResult Execute(
        ApplicationSessionState session,
        CombatEndTurnIntent intent)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(intent);

        return WatchtowerCombatResultMapper.ToCombatResolutionResult(
            WatchtowerCombatOrchestrator.Execute(
                session,
                new WatchtowerCombatEndTurnIntent
                {
                    ExpectedEncounterRevision = intent.ExpectedEncounterRevision,
                    ActorCombatantId = intent.ActorCombatantId
                }));
    }
}
