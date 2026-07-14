namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterActionEvaluation
{
    public required string ActionOptionId { get; init; }

    public required string ActorCombatantId { get; init; }

    public required long EncounterRevision { get; init; }

    public required bool IsCommonlyLegal { get; init; }

    public required EncounterActionUnavailabilityReason
        UnavailabilityReason { get; init; }
}
