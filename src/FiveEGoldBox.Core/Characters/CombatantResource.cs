namespace FiveEGoldBox.Core.Characters;

/// Something a combatant can spend during an encounter and has a finite
/// amount of.
///
/// Projected in the way ammunition is: what the character persistently holds
/// becomes what the encounter can spend, and what is left comes back out
/// afterwards. The encounter neither knows nor cares that a spell slot is
/// recovered on a long rest.
public sealed record CombatantResource
{
    public required string ResourceId { get; init; }

    public required int Remaining { get; init; }

    public required int Maximum { get; init; }
}
