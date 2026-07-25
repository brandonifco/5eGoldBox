namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// What a scenario demands of the party attempting it — a predicate over
/// whatever party the campaign supplied, never a roster of its own.
internal sealed record PartyRequirementDefinition
{
    internal required int MinimumMembers { get; init; }

    internal required int MaximumMembers { get; init; }

    /// Members who must still be able to act for the scenario to be attempted.
    internal required int MinimumConsciousMembers { get; init; }
}
