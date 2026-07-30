namespace FiveEGoldBox.Application.Scenarios;

/// Named IDs into the Hollow Mill scenario that other code (tests, the
/// campaign roster) refers to by name rather than by a bare string literal.
///
/// These used to live on HollowMillScenarioDefinitionProvider alongside
/// the C# that built the scenario itself. That builder is gone --
/// data/scenarios/hollow-mill/scenario.json is authoritative now, loaded
/// through ScenarioDefinitionRegistry -- but the IDs are not hardcoded
/// content, just names for entries the JSON already declares.
internal static class HollowMillScenarioIds
{
    internal const string ScenarioId = "scenario.hollow-mill";

    internal const string VillageLocationId = "location.hollow-mill-village";

    internal const string MillLocationId = "location.hollow-mill-house";

    internal const string RouteId = "route.village-to-mill";

    internal const string ShortcutRouteId = "route.village-to-mill.shortcut";

    internal const string RumorHeard = "mill.rumor-heard";

    internal const string CommissionAccepted = "mill.commission-accepted";

    internal const string HerbalistConsulted = "mill.herbalist-consulted";

    internal const string MillerConfronted = "mill.miller-confronted";

    internal const string VerminRoused = "mill.vermin-roused";

    internal const string MillersLost = "mill.millers-lost";

    internal const string BlightSevered = "mill.blight-severed";

    internal const string InformedEncounterId = "encounter.mill-vermin-swarm";

    internal const string BlindEncounterId = "encounter.mill-thrall-ambush";

    internal const string VerminSideId = "side.mill-vermin";
}
