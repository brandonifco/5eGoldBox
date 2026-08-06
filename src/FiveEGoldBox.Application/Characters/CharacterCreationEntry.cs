using FiveEGoldBox.Core.Characters;

namespace FiveEGoldBox.Application.Characters;

/// One character a player is submitting for a new party: the stable ID it
/// will carry as a party member, paired with the build choices that ID
/// resolves to. The ID is the caller's to assign (a UI-generated slug or
/// GUID, say) rather than something CharacterCreationRules invents, since a
/// party member's own identity is a session/UI concern, not a rules one.
public sealed record CharacterCreationEntry
{
    public required string PartyMemberId { get; init; }

    public required CharacterDraft Draft { get; init; }
}
