namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveScenarioStateV1
{
    /// The scenario's own progress marker, stored as the opaque string the
    /// engine carries at runtime.
    ///
    /// This was a Watchtower-specific enum. Its wire form was already the
    /// member name, and progress IDs are those names verbatim, so widening it
    /// to a string leaves existing V1 documents byte-identical while letting
    /// any scenario's vocabulary round-trip.
    public required string Progress { get; init; }
}
