using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Scenarios.Definitions;

/// A static NPC the party can talk to for a single line of flavor text.
/// Deliberately minimal -- no branching, no quest state, no shop -- per
/// docs/Current Game Development Goals.txt's own guidance to implement the
/// content types the current chapter needs rather than get ahead of it. An
/// NPC blocks its own tile like a door, approached from an adjacent square
/// rather than stood upon.
internal sealed record NpcDefinition
{
    internal required string NpcId { get; init; }

    internal required GridPosition Position { get; init; }

    internal required string Name { get; init; }

    internal required string DialogueText { get; init; }
}
