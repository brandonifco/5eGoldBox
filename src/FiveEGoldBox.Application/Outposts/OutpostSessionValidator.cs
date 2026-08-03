using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

internal static class OutpostSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.RegionalTravel,
            "An outpost session cannot contain regional-travel state.");

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.Exploration,
            "An outpost session cannot contain exploration state.");

        SessionSubstateInvariant.RequireAbsent(
            state,
            state.ActiveEncounter,
            "An outpost session cannot contain an active encounter.");
    }
}
