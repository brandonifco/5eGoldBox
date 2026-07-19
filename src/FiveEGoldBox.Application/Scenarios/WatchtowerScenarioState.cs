namespace FiveEGoldBox.Application.Scenarios;

public sealed record WatchtowerScenarioState
{
    public required WatchtowerScenarioProgress Progress { get; init; }
}
