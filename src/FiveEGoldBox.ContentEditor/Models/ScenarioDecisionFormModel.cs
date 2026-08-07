using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// A decision is a prompt at a location with two or more options. Each option
/// may advance progress or not -- a declining option ("Not yet") legitimately
/// advances nothing, which is why ResultingProgressId is optional and "" is
/// normalized back to null so the property is omitted entirely.
internal sealed class ScenarioDecisionFormModel
{
    public string DecisionId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string LocationId { get; set; } = "";

    public List<string> RequiredProgressIds { get; set; } = [];

    public List<ScenarioDecisionOptionFormModel> Options { get; set; } = [];

    public static ScenarioDecisionFormModel FromDefinition(
        ScenarioDecisionDefinitionV1 decision)
    {
        return new ScenarioDecisionFormModel
        {
            DecisionId = decision.DecisionId,
            DisplayName = decision.DisplayName,
            LocationId = decision.LocationId,
            RequiredProgressIds = decision.RequiredProgressIds.ToList(),
            Options = decision.Options
                .Select(ScenarioDecisionOptionFormModel.FromDefinition)
                .ToList()
        };
    }

    public ScenarioDecisionDefinitionV1 ToDefinition()
    {
        return new ScenarioDecisionDefinitionV1
        {
            DecisionId = DecisionId,
            DisplayName = DisplayName,
            LocationId = LocationId,
            RequiredProgressIds = RequiredProgressIds,
            Options = Options.Select(option => option.ToDefinition()).ToList()
        };
    }
}

internal sealed class ScenarioDecisionOptionFormModel
{
    public string OptionId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string ResultingProgressId { get; set; } = "";

    public static ScenarioDecisionOptionFormModel FromDefinition(
        ScenarioDecisionOptionDefinitionV1 option)
    {
        return new ScenarioDecisionOptionFormModel
        {
            OptionId = option.OptionId,
            DisplayName = option.DisplayName,
            ResultingProgressId = option.ResultingProgressId ?? ""
        };
    }

    public ScenarioDecisionOptionDefinitionV1 ToDefinition()
    {
        return new ScenarioDecisionOptionDefinitionV1
        {
            OptionId = OptionId,
            DisplayName = DisplayName,
            ResultingProgressId = string.IsNullOrWhiteSpace(ResultingProgressId)
                ? null
                : ResultingProgressId
        };
    }
}
