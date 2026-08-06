using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Content.V1;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.ContentEditor.Services;

/// One scenario summary for the landing page -- just enough to list and
/// link into a scenario without loading its full content.
internal sealed record ScenarioSummary(
    string ScenarioId,
    string DisplayName,
    string FilePath);

/// The seam every Scenario page saves and loads through, mirroring
/// RulesetContentService's own write-and-validate discipline exactly: every
/// write re-reads the file fresh, renders just the one array being changed
/// (ScenarioJsonFormatting), splices it into a fresh copy of the file's
/// bytes (RulesetJsonSplicer) written to a temp file, validates the temp
/// file through the public ContentPackValidation.ValidateScenarioPack
/// facade the standalone `validate` console command uses, and only then
/// replaces the real file.
///
/// Unlike the single hardcoded ruleset file, a scenario is one file per
/// scenario, so every method here takes the target file's path explicitly
/// rather than the service owning one fixed path -- callers get that path
/// from ListScenarios()/FindScenario() (backed by
/// RepositoryLocator.ResolveScenarioPackPaths()).
///
/// This class -- and every method here that touches the scenario V1 DTOs --
/// is internal, not public: ScenarioLocationDefinitionV1/
/// TravelRouteDefinitionV1/ShopDefinitionV1 are themselves internal to
/// FiveEGoldBox.Application (reachable here only via the InternalsVisibleTo
/// grant), so a public method with an internal parameter/return type would
/// be an accessibility-consistency error. Every caller (this project's own
/// Razor components) lives in the same assembly, so internal is all that's
/// needed.
internal sealed class ScenarioContentService
{
    // ----- Scenario listing -----

    internal IReadOnlyList<ScenarioSummary> ListScenarios()
    {
        return RepositoryLocator.ResolveScenarioPackPaths()
            .Select(path =>
            {
                ScenarioPackDocument document = ScenarioPackDocument.Read(path);
                return new ScenarioSummary(document.ScenarioId, document.DisplayName, path);
            })
            .ToList();
    }

    internal ScenarioSummary? FindScenario(
        string scenarioId)
    {
        return ListScenarios().FirstOrDefault(summary => summary.ScenarioId == scenarioId);
    }

    internal IReadOnlyList<string> LoadProgressIds(
        string scenarioFilePath)
    {
        return ScenarioPackDocument.Read(scenarioFilePath).ProgressIds;
    }

    // ----- Locations -----

    internal IReadOnlyList<ScenarioLocationDefinitionV1> LoadLocations(
        string scenarioFilePath)
    {
        return ScenarioPackDocument.Read(scenarioFilePath).Locations;
    }

    internal ScenarioLocationDefinitionV1? FindLocation(
        string scenarioFilePath,
        string locationId)
    {
        return LoadLocations(scenarioFilePath)
            .FirstOrDefault(location => location.LocationId == locationId);
    }

    internal ValidationResult SaveLocation(
        string scenarioFilePath,
        ScenarioLocationDefinitionV1 location)
    {
        ScenarioPackDocument document = ScenarioPackDocument.Read(scenarioFilePath);
        List<ScenarioLocationDefinitionV1> updated = Upsert(
            document.Locations,
            location,
            l => l.LocationId);

        return WriteAndValidate(
            scenarioFilePath,
            "Locations",
            ScenarioJsonFormatting.RenderLocations(
                updated,
                (locationId, _) => document.FindRawExplorationMapText(locationId)));
    }

    internal ValidationResult DeleteLocation(
        string scenarioFilePath,
        string locationId)
    {
        ScenarioPackDocument document = ScenarioPackDocument.Read(scenarioFilePath);
        List<ScenarioLocationDefinitionV1> updated = document.Locations
            .Where(location => location.LocationId != locationId)
            .ToList();

        return WriteAndValidate(
            scenarioFilePath,
            "Locations",
            ScenarioJsonFormatting.RenderLocations(
                updated,
                (locationId, _) => document.FindRawExplorationMapText(locationId)));
    }

    // ----- ExplorationMap -----

    internal ExplorationMapDefinitionV1? FindExplorationMap(
        string scenarioFilePath,
        string locationId)
    {
        return FindLocation(scenarioFilePath, locationId)?.ExplorationMap;
    }

    /// Unlike SaveLocation, which leaves every map byte-identical, this
    /// rewrites the named location's map from the supplied definition. Every
    /// other location's map still copies through verbatim, so editing one
    /// map never reformats a sibling's.
    internal ValidationResult SaveExplorationMap(
        string scenarioFilePath,
        string locationId,
        ExplorationMapDefinitionV1 map)
    {
        ScenarioPackDocument document = ScenarioPackDocument.Read(scenarioFilePath);

        return WriteAndValidate(
            scenarioFilePath,
            "Locations",
            ScenarioJsonFormatting.RenderLocations(
                document.Locations,
                (id, column) => id == locationId
                    ? ScenarioJsonFormatting.RenderExplorationMap(map, column)
                    : document.FindRawExplorationMapText(id)));
    }

    // ----- Routes -----

    internal IReadOnlyList<TravelRouteDefinitionV1> LoadRoutes(
        string scenarioFilePath)
    {
        return ScenarioPackDocument.Read(scenarioFilePath).Routes;
    }

    internal TravelRouteDefinitionV1? FindRoute(
        string scenarioFilePath,
        string routeId)
    {
        return LoadRoutes(scenarioFilePath)
            .FirstOrDefault(route => route.RouteId == routeId);
    }

    internal ValidationResult SaveRoute(
        string scenarioFilePath,
        TravelRouteDefinitionV1 route)
    {
        List<TravelRouteDefinitionV1> updated = Upsert(
            LoadRoutes(scenarioFilePath),
            route,
            r => r.RouteId);

        return WriteAndValidate(
            scenarioFilePath,
            "Routes",
            ScenarioJsonFormatting.RenderRoutes(updated));
    }

    internal ValidationResult DeleteRoute(
        string scenarioFilePath,
        string routeId)
    {
        List<TravelRouteDefinitionV1> updated = LoadRoutes(scenarioFilePath)
            .Where(route => route.RouteId != routeId)
            .ToList();

        return WriteAndValidate(
            scenarioFilePath,
            "Routes",
            ScenarioJsonFormatting.RenderRoutes(updated));
    }

    // ----- Shops -----

    internal IReadOnlyList<ShopDefinitionV1> LoadShops(
        string scenarioFilePath)
    {
        return ScenarioPackDocument.Read(scenarioFilePath).Shops;
    }

    internal ShopDefinitionV1? FindShop(
        string scenarioFilePath,
        string shopId)
    {
        return LoadShops(scenarioFilePath)
            .FirstOrDefault(shop => shop.ShopId == shopId);
    }

    internal ValidationResult SaveShop(
        string scenarioFilePath,
        ShopDefinitionV1 shop)
    {
        List<ShopDefinitionV1> updated = Upsert(
            LoadShops(scenarioFilePath),
            shop,
            s => s.ShopId);

        return WriteAndValidateShops(scenarioFilePath, updated);
    }

    internal ValidationResult DeleteShop(
        string scenarioFilePath,
        string shopId)
    {
        List<ShopDefinitionV1> updated = LoadShops(scenarioFilePath)
            .Where(shop => shop.ShopId != shopId)
            .ToList();

        return WriteAndValidateShops(scenarioFilePath, updated);
    }

    // ----- Shared write path -----

    private static List<T> Upsert<T>(
        IReadOnlyList<T> existing,
        T value,
        Func<T, string> idSelector)
    {
        string id = idSelector(value);
        List<T> updated = [.. existing];
        int index = updated.FindIndex(item => idSelector(item) == id);

        if (index >= 0)
        {
            updated[index] = value;
        }
        else
        {
            updated.Add(value);
        }

        return updated;
    }

    private static ValidationResult WriteAndValidate(
        string scenarioFilePath,
        string rootPropertyName,
        string replacementArrayText)
    {
        byte[] original = File.ReadAllBytes(scenarioFilePath);
        byte[] updated = RulesetJsonSplicer.ReplaceRootPropertyValue(
            original,
            rootPropertyName,
            replacementArrayText);

        return WriteTempAndCommit(scenarioFilePath, updated);
    }

    /// Shops is the one root property that may not exist yet in a scenario
    /// file (ScenarioPackV1.Shops defaults to empty, and no scenario was
    /// ever authored with an explicit empty "Shops" key) -- see
    /// RulesetJsonSplicer.ReplaceOrInsertRootPropertyValue's own header
    /// comment for why "Decisions" (always present) is the anchor.
    private static ValidationResult WriteAndValidateShops(
        string scenarioFilePath,
        IReadOnlyList<ShopDefinitionV1> updatedShops)
    {
        byte[] original = File.ReadAllBytes(scenarioFilePath);
        byte[] updated = RulesetJsonSplicer.ReplaceOrInsertRootPropertyValue(
            original,
            "Shops",
            ScenarioJsonFormatting.RenderShops(updatedShops),
            anchorPropertyName: "Decisions");

        return WriteTempAndCommit(scenarioFilePath, updated);
    }

    private static ValidationResult WriteTempAndCommit(
        string scenarioFilePath,
        byte[] updatedBytes)
    {
        string tempPath = scenarioFilePath + ".tmp-" + Guid.NewGuid().ToString("N");

        try
        {
            File.WriteAllBytes(tempPath, updatedBytes);

            ValidationResult validation = ContentPackValidation.ValidateScenarioPack(tempPath);

            if (validation.IsValid)
            {
                File.Copy(tempPath, scenarioFilePath, overwrite: true);
            }

            return validation;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
