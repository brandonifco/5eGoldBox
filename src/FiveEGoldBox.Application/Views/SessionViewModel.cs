using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Views;

/// Everything a client needs to draw the current moment.
public sealed record SessionViewModel
{
    public required string ScenarioDisplayName { get; init; }

    public required ApplicationMode Mode { get; init; }

    public required string LocationId { get; init; }

    /// The location's authored name, so a client shows "Harbor Town" rather
    /// than "location.harbor-town".
    public required string LocationDisplayName { get; init; }

    /// The scenario's own progress marker. Opaque to the client; useful for
    /// debugging and for save slots.
    public required string ProgressId { get; init; }

    /// True once the scenario has ended, whether well or badly.
    public required bool IsConcluded { get; init; }

    /// Null unless the scenario has ended.
    public bool? IsSuccess { get; init; }

    /// The scenario's own authored epilogue for this ending. Null unless
    /// the scenario has ended.
    public string? ConclusionText { get; init; }

    public required IReadOnlyList<SessionAction> Actions { get; init; }

    public required PartyViewModel Party { get; init; }
}
