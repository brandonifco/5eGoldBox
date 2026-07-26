using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Outposts;

internal static class OutpostSessionValidator
{
    internal static void Validate(
        ApplicationSessionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.RegionalTravel is not null)
        {
            throw new ArgumentException(
                "An outpost session cannot contain regional-travel state.",
                nameof(state));
        }

        if (state.Exploration is not null)
        {
            throw new ArgumentException(
                "An outpost session cannot contain exploration state.",
                nameof(state));
        }

        if (state.ActiveEncounter is not null)
        {
            throw new ArgumentException(
                "An outpost session cannot contain an active encounter.",
                nameof(state));
        }
    }
}
