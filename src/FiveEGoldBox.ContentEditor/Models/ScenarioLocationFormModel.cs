using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// A mutable, two-way-bindable stand-in for ScenarioLocationDefinitionV1 --
/// see WeaponFormModel's header comment for why this shape exists. Internal,
/// not public, unlike the ruleset form models: ScenarioLocationDefinitionV1
/// is itself internal to FiveEGoldBox.Application (reachable here only via
/// the InternalsVisibleTo grant), and a public method with an internal
/// parameter/return type is an accessibility-consistency error.
///
/// This form deliberately has no ExplorationMap field -- that content isn't
/// editable through this editor yet. ToDefinition() below always leaves it
/// null, but that's harmless: ScenarioJsonFormatting.RenderLocation never
/// reads a location's ExplorationMap property at all, instead copying the
/// original file's exact bytes for that location's map by LocationId (see
/// its own header comment), so what ToDefinition() puts there is never
/// actually written.
internal sealed class ScenarioLocationFormModel
{
    public string LocationId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public List<string> ExplorableProgressIds { get; set; } = [];

    public static ScenarioLocationFormModel FromDefinition(
        ScenarioLocationDefinitionV1 location)
    {
        return new ScenarioLocationFormModel
        {
            LocationId = location.LocationId,
            DisplayName = location.DisplayName,
            ExplorableProgressIds = [.. location.ExplorableProgressIds]
        };
    }

    public ScenarioLocationDefinitionV1 ToDefinition()
    {
        return new ScenarioLocationDefinitionV1
        {
            LocationId = LocationId,
            DisplayName = DisplayName,
            ExplorationMap = null,
            ExplorableProgressIds = ExplorableProgressIds
        };
    }
}
