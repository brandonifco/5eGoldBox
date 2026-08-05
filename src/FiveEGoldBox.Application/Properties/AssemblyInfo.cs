using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FiveEGoldBox.Application.Tests")]

// The console's tests construct combat view models directly to cover states the
// facade will not produce, such as a decision still awaiting automatic
// processing. Those types validate their own shape through internal
// constructors, so the test project needs the same access.
[assembly: InternalsVisibleTo("FiveEGoldBox.Console.Tests")]

// The scenario/campaign content editor deserializes scenario and campaign
// JSON directly into the V1 pack DTOs (ScenarioPackV1, CampaignPackV1, and
// their nested types) the same way it already deserializes ruleset content
// into the public Core.Definitions types -- those DTOs already mirror the
// JSON schema field-for-field, so this avoids hand-rolling a second,
// drift-prone set of mirror types just to work around their visibility.
[assembly: InternalsVisibleTo("FiveEGoldBox.ContentEditor")]
[assembly: InternalsVisibleTo("FiveEGoldBox.ContentEditor.Tests")]
