namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveHitPointsV1
{
    public required int MaximumHitPoints { get; init; }

    public required int CurrentHitPoints { get; init; }

    public required int TemporaryHitPoints { get; init; }
}
