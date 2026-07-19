namespace FiveEGoldBox.Application.Travel;

internal static class WatchtowerRegionalRoute
{
    internal const string RouteId =
        "route.outpost-watchtower";

    internal const string WatchtowerLocationId =
        "location.ruined-watchtower";

    internal const int FinalStepIndex = 3;

    internal static bool HasSupportedEndpoints(
        string originLocationId,
        string destinationLocationId)
    {
        if (string.IsNullOrWhiteSpace(originLocationId)
            || string.IsNullOrWhiteSpace(destinationLocationId)
            || string.Equals(
                originLocationId,
                destinationLocationId,
                StringComparison.Ordinal))
        {
            return false;
        }

        bool originIsWatchtower = string.Equals(
            originLocationId,
            WatchtowerLocationId,
            StringComparison.Ordinal);
        bool destinationIsWatchtower = string.Equals(
            destinationLocationId,
            WatchtowerLocationId,
            StringComparison.Ordinal);

        return originIsWatchtower != destinationIsWatchtower;
    }
}
