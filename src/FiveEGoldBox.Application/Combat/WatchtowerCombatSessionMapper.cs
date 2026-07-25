using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Moves between the encounter the combat components work on and the session
/// that owns it. Every write goes back through ApplicationSessionRules, so a
/// session that fails its mode invariants can never be produced by combat.
internal static class WatchtowerCombatSessionMapper
{
    internal static ApplicationSessionState Canonicalize(
        ApplicationSessionState source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ApplicationSessionRules.CreateCanonical(source);
    }

    internal static EncounterState GetEncounter(
        ApplicationSessionState state)
    {
        return state.ActiveEncounter?.Encounter
            ?? throw new InvalidOperationException(
                "The watchtower combat session has no active encounter context.");
    }

    internal static ApplicationSessionState ReplaceEncounter(
        ApplicationSessionState state,
        EncounterState encounter,
        int randomValuesConsumed)
    {
        ApplicationSessionState replacement = state with
        {
            RandomValuesConsumed = randomValuesConsumed,
            ActiveEncounter = state.ActiveEncounter! with
            {
                Encounter = encounter
            }
        };

        return ApplicationSessionRules.CreateCanonical(replacement);
    }
}
