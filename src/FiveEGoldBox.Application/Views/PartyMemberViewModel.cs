namespace FiveEGoldBox.Application.Views;

/// One party member, for a client to draw a real Character screen from.
public sealed record PartyMemberViewModel
{
    public required string PartyMemberId { get; init; }

    public required string DisplayName { get; init; }

    public required string ClassDisplayName { get; init; }

    public required int CurrentHitPoints { get; init; }

    public required int MaximumHitPoints { get; init; }
}
