namespace FiveEGoldBox.Core.Rules;

public sealed record HitPointState
{
    public required int MaximumHitPoints { get; init; }

    public required int CurrentHitPoints { get; init; }

    public required int TemporaryHitPoints { get; init; }

    public bool IsAtZeroHitPoints => CurrentHitPoints == 0;
}