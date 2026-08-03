namespace FiveEGoldBox.Application.Scenarios.Definitions;

internal sealed record ScenarioConclusionDefinition
{
    internal required string ProgressId { get; init; }

    internal required bool IsSuccess { get; init; }

    /// Where the party stands when the scenario ends on this marker.
    internal required string LocationId { get; init; }

    /// What the player is actually told the scenario ended with -- a
    /// concluded scenario used to offer nothing beyond a bare progress-ID
    /// flip. One authored sentence, no branching, matching
    /// NpcDefinition.DialogueText's own precedent for real-but-minimal
    /// content.
    internal required string EpilogueText { get; init; }
}
