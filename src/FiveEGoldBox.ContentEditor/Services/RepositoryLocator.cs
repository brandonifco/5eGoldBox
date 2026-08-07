namespace FiveEGoldBox.ContentEditor.Services;

/// Finds the repository root and the single ruleset pack file this editor
/// targets, at runtime.
///
/// Mirrors ContentPackSchemaWriter.ResolveRepositoryRoot()
/// (tests/FiveEGoldBox.Application.Tests/ContentPackSchemaWriter.cs) exactly
/// -- walk up parent directories from AppContext.BaseDirectory looking for
/// 5eGoldBox.sln -- rather than depending on Application's internal
/// DataDirectoryLocator, which this tool has no InternalsVisibleTo access
/// to. This is an already-established, working precedent for exactly this
/// "a tool outside the normal load pipeline needs to find a data file"
/// problem.
public static class RepositoryLocator
{
    public static string ResolveRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string solutionPath = Path.Combine(
                directory.FullName,
                "5eGoldBox.sln");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate 5eGoldBox.sln by walking upward from '{AppContext.BaseDirectory}'.");
    }

    public static string ResolveCoreRulesetPackPath()
    {
        return Path.Combine(
            ResolveRepositoryRoot(),
            "data",
            "rulesets",
            "campaign",
            "core.json");
    }

    /// Campaign content is one file per campaign, same directory-scan shape
    /// as scenarios -- data/campaigns/&lt;id&gt;/campaign.json.
    public static IReadOnlyList<string> ResolveCampaignPackPaths()
    {
        string campaignsRoot = Path.Combine(ResolveRepositoryRoot(), "data", "campaigns");

        if (!Directory.Exists(campaignsRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(campaignsRoot)
            .Select(directory => Path.Combine(directory, "campaign.json"))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    /// Unlike the single hardcoded ruleset file, scenario content is one
    /// file per scenario -- data/scenarios/<id>/scenario.json -- so this
    /// scans the directory rather than resolving one fixed path. Sorted by
    /// path for a stable listing order across runs.
    public static IReadOnlyList<string> ResolveScenarioPackPaths()
    {
        string scenariosRoot = Path.Combine(ResolveRepositoryRoot(), "data", "scenarios");

        if (!Directory.Exists(scenariosRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(scenariosRoot)
            .Select(directory => Path.Combine(directory, "scenario.json"))
            .Where(File.Exists)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }
}
