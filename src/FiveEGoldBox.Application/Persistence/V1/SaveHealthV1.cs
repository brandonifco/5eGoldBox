namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveHealthV1
{
    public required SaveHitPointsV1 HitPoints { get; init; }

    public required SaveDeathSavingThrowsV1 DeathSavingThrows { get; init; }

    public required bool IsInstantlyDead { get; init; }
}
