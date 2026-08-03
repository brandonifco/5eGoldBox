namespace FiveEGoldBox.Application.Sessions;

/// Every session-mode validator enforces the same shape: of the three
/// mutually exclusive substates (`RegionalTravel`/`Exploration`/
/// `ActiveEncounter`), only the one that mode actually uses may be
/// populated. Each validator still names its own substates and messages -
/// this only replaces the repeated null-check-and-throw block itself.
internal static class SessionSubstateInvariant
{
    internal static void RequireAbsent(
        ApplicationSessionState state,
        object? substateValue,
        string message)
    {
        if (substateValue is not null)
        {
            throw new ArgumentException(message, nameof(state));
        }
    }
}
