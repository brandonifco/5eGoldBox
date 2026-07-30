using FiveEGoldBox.Application.Content.Schema;

namespace FiveEGoldBox.Application.Tests;

/// Writes the generated content-pack JSON schemas to data/schemas/, so
/// regenerating one after a V1 DTO changes is a deliberate act somebody
/// repeats, matching FixtureWriter's own precedent for the save fixtures.
/// Skipped in a normal pass: a schema that regenerates itself proves
/// nothing -- ContentPackSchemaTests is the one that actually checks for
/// drift against what is checked in.
public sealed class ContentPackSchemaWriter
{
    [Fact(Skip = "Run by hand when a schema is deliberately regenerated.")]
    public void WriteContentPackSchemas()
    {
        string schemasDirectory = Path.Combine(
            ResolveRepositoryRoot(),
            "data",
            "schemas");

        Directory.CreateDirectory(schemasDirectory);

        Write(
            schemasDirectory,
            "ruleset-pack.schema.json",
            ContentPackSchemas.RulesetPackSchema());
        Write(
            schemasDirectory,
            "scenario-pack.schema.json",
            ContentPackSchemas.ScenarioPackSchema());
        Write(
            schemasDirectory,
            "campaign-pack.schema.json",
            ContentPackSchemas.CampaignPackSchema());
    }

    private static void Write(
        string directory,
        string fileName,
        string content)
    {
        File.WriteAllText(
            Path.Combine(directory, fileName),
            content);
    }

    private static string ResolveRepositoryRoot()
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
}
