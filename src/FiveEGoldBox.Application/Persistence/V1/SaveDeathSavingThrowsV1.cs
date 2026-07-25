namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveDeathSavingThrowsV1
{
    public required int SuccessCount { get; init; }

    public required int FailureCount { get; init; }

    public required bool IsStable { get; init; }
}
