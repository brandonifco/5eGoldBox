using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Parties;

public sealed record PartyMemberState
{
    public required string PartyMemberId { get; init; }

    public required string CharacterDefinitionId { get; init; }

    public required string DisplayName { get; init; }

    public required string ClassId { get; init; }

    public required CombatantZeroHitPointPolicy ZeroHitPointPolicy { get; init; }

    public required CombatantHealthState Health { get; init; }

    public AmmunitionState? Ammunition { get; init; }
}
