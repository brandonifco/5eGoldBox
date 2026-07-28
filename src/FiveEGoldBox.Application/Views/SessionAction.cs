using FiveEGoldBox.Application.Outposts;

namespace FiveEGoldBox.Application.Views;

/// One thing the party can do, and what to call it on screen.
///
/// The display name comes from the scenario's own content, so a client renders
/// "Sign the charter" or "Enter the Sunken Chapel" without holding a table of
/// its own.
public sealed record SessionAction
{
    public required SessionActionKind Kind { get; init; }

    public required string DisplayName { get; init; }

    /// Set when the action is one of several options on the same decision, so
    /// the client can pass it back to <see cref="OutpostMissionRules.Resolve"/>.
    public OutpostMissionChoice? MissionChoice { get; init; }

    /// Set when the action begins a journey along a specific route, so the
    /// client can pass it back to
    /// <see cref="FiveEGoldBox.Application.Travel.RegionalTravelRules.BeginJourney"/>.
    /// Only meaningful when more than one route is open — see
    /// <see cref="FiveEGoldBox.Application.Travel.RegionalTravelRules.GetAvailableRoutes"/>.
    public string? RouteId { get; init; }
}
