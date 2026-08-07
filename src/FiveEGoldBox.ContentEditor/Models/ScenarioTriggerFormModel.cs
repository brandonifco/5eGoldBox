using FiveEGoldBox.Application.Content.V1;

namespace FiveEGoldBox.ContentEditor.Models;

/// A trigger is either fixed to one square (Floor + Position both set) or
/// fires anywhere in its location (both absent). The DTO makes them
/// independently optional and the validator only checks each when present,
/// but authoring them independently has no meaning -- a Position with no
/// Floor is ungated on a multi-floor map -- so this model collapses the pair
/// behind one IsFixedToASquare flag and writes both or neither.
internal sealed class ScenarioTriggerFormModel
{
    public string TriggerId { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string LocationId { get; set; } = "";

    public bool IsFixedToASquare { get; set; }

    public string Floor { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public List<string> RequiredProgressIds { get; set; } = [];

    public string ResultingProgressId { get; set; } = "";

    /// "" means no encounter, which is the common case -- most triggers just
    /// advance progress. Normalized back to null on the way out so the
    /// property is omitted rather than written empty.
    public string EncounterId { get; set; } = "";

    public static ScenarioTriggerFormModel FromDefinition(
        ScenarioTriggerDefinitionV1 trigger)
    {
        return new ScenarioTriggerFormModel
        {
            TriggerId = trigger.TriggerId,
            DisplayName = trigger.DisplayName,
            LocationId = trigger.LocationId,
            IsFixedToASquare = trigger.Position is not null,
            Floor = trigger.Floor ?? "",
            X = trigger.Position?.X ?? 0,
            Y = trigger.Position?.Y ?? 0,
            RequiredProgressIds = trigger.RequiredProgressIds.ToList(),
            ResultingProgressId = trigger.ResultingProgressId,
            EncounterId = trigger.EncounterId ?? ""
        };
    }

    public ScenarioTriggerDefinitionV1 ToDefinition()
    {
        return new ScenarioTriggerDefinitionV1
        {
            TriggerId = TriggerId,
            DisplayName = DisplayName,
            LocationId = LocationId,
            Floor = IsFixedToASquare && !string.IsNullOrWhiteSpace(Floor) ? Floor : null,
            Position = IsFixedToASquare ? new GridPositionV1 { X = X, Y = Y } : null,
            RequiredProgressIds = RequiredProgressIds,
            ResultingProgressId = ResultingProgressId,
            EncounterId = string.IsNullOrWhiteSpace(EncounterId) ? null : EncounterId
        };
    }
}
