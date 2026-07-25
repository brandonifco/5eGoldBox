namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveScenarioStateV1
{
    public required SaveScenarioProgressV1 Progress { get; init; }
}
