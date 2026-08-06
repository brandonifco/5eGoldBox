using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// Decides whether the party is standing where an authored trigger fires.
///
/// A trigger's conditions are all optional except its location, so one fixed to
/// a square is checked strictly while one covering a whole location fires
/// anywhere in it. Location only -- which way the party is facing never
/// gates a trigger, so a player never has to guess a direction to find one.
internal static class ScenarioTriggerMatcher
{
    /// The trigger the party could act on right now, if any.
    internal static ScenarioTriggerDefinition? FindAvailable(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Triggers
            .FirstOrDefault(trigger => IsAvailable(trigger, session));
    }

    internal static bool IsAvailable(
        ScenarioTriggerDefinition trigger,
        ApplicationSessionState session)
    {
        return string.Equals(
                trigger.LocationId,
                session.CurrentLocationId,
                StringComparison.Ordinal)
            && trigger.RequiredProgressIds.Contains(
                session.Scenario.ProgressId,
                StringComparer.Ordinal)
            && MatchesPosition(trigger, session.Exploration);
    }

    /// Whether an exploration state satisfies a trigger's positional
    /// conditions. Used both to offer a trigger and, afterwards, to check that
    /// a recorded return context is where the trigger actually fired.
    internal static bool MatchesPosition(
        ScenarioTriggerDefinition trigger,
        ExplorationState? exploration)
    {
        if (trigger.Floor is null
            && trigger.Position is null)
        {
            return true;
        }

        if (exploration is null)
        {
            return false;
        }

        return (trigger.Floor is null
                || exploration.Floor == trigger.Floor)
            && (trigger.Position is null
                || exploration.Position == trigger.Position);
    }

    /// The trigger that starts a given encounter, used when validating that a
    /// session in combat got there legitimately.
    internal static ScenarioTriggerDefinition? FindForEncounter(
        ApplicationSessionState session,
        string encounterId)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ScenarioDefinitionRegistry
            .Resolve(session)
            .Triggers
            .FirstOrDefault(trigger => string.Equals(
                trigger.EncounterId,
                encounterId,
                StringComparison.Ordinal));
    }
}
