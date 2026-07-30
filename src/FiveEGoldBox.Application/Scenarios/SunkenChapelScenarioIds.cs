namespace FiveEGoldBox.Application.Scenarios;

/// Named IDs into the Sunken Chapel scenario that other code (tests, the
/// campaign roster) refers to by name rather than by a bare string literal.
///
/// These used to live on SunkenChapelScenarioDefinitionProvider alongside
/// the C# that built the scenario itself. That builder is gone --
/// data/scenarios/sunken-chapel/scenario.json is authoritative now, loaded
/// through ScenarioDefinitionRegistry -- but the IDs are not hardcoded
/// content, just names for entries the JSON already declares.
internal static class SunkenChapelScenarioIds
{
    internal const string ScenarioId = "scenario.sunken-chapel";

    internal const string HarborLocationId = "location.harbor-town";

    internal const string ChapelLocationId = "location.sunken-chapel";

    internal const string RouteId = "route.harbor-to-chapel";

    internal const string RumourHeard = "chapel.rumour-heard";

    internal const string CharterSigned = "chapel.charter-signed";

    internal const string SealBroken = "chapel.seal-broken";

    internal const string GuardiansRoused = "chapel.guardians-roused";

    internal const string GuardiansBanished = "chapel.guardians-banished";

    internal const string RelicRecovered = "chapel.relic-recovered";

    internal const string EncounterId = "encounter.chapel-guardians";

    internal const string GuardianSideId = "side.chapel-guardians";

    internal const string GuardianDaggerId = "weapon.chapel-guardian.dagger";
}
