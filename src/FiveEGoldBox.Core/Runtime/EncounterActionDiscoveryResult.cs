namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterActionDiscoveryResult
{
    public required string EncounterId { get; init; }

    public required long EncounterRevision { get; init; }

    public required IReadOnlyList<EncounterActionEvaluation>
        Evaluations { get; init; }
}
