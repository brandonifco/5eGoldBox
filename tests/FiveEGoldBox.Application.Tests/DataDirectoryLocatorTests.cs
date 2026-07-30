using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Scenarios;

namespace FiveEGoldBox.Application.Tests;

/// FIVEEGOLDBOX_DATA_ROOT is process-global environment state, and xunit
/// runs different test classes in parallel by default -- so every test here
/// that sets it first warms the three registries' caches for every key this
/// repo currently declares. Once cached, a registry never touches
/// DataDirectoryLocator again, which closes the race for the rest of the
/// run regardless of what other tests do concurrently afterward.
public sealed class DataDirectoryLocatorTests
{
    [Fact]
    public void ResolveDataFilePath_WalksUpToFindTheRepositoryCheckout()
    {
        string resolved = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("rulesets", "campaign", "core.json"));

        Assert.True(File.Exists(resolved));
        Assert.EndsWith(
            Path.Combine("data", "rulesets", "campaign", "core.json"),
            resolved);
    }

    [Fact]
    public void ResolveDataFilePath_WithNoMatchAnywhere_ThrowsClearly()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => DataDirectoryLocator.ResolveDataFilePath(
                Path.Combine("nonexistent", "made-up.json")));

        Assert.Contains(
            DataDirectoryLocator.DataRootEnvironmentVariable,
            exception.Message);
    }

    [Fact]
    public void ResolveDataFilePath_WithEnvironmentVariableSet_PrefersIt()
    {
        WarmUpEveryRealRegistryEntry();

        string tempRoot = Directory.CreateTempSubdirectory().FullName;

        try
        {
            const string relativePath = "pack.json";
            string fullPath = Path.Combine(tempRoot, relativePath);
            File.WriteAllText(fullPath, "{}");

            Environment.SetEnvironmentVariable(
                DataDirectoryLocator.DataRootEnvironmentVariable,
                tempRoot);

            string resolved = DataDirectoryLocator.ResolveDataFilePath(
                relativePath);

            Assert.Equal(fullPath, resolved);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DataDirectoryLocator.DataRootEnvironmentVariable,
                null);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveDataFilePath_WithEnvironmentVariableSetButFileMissing_ThrowsClearly()
    {
        WarmUpEveryRealRegistryEntry();

        string tempRoot = Directory.CreateTempSubdirectory().FullName;

        try
        {
            Environment.SetEnvironmentVariable(
                DataDirectoryLocator.DataRootEnvironmentVariable,
                tempRoot);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => DataDirectoryLocator.ResolveDataFilePath("missing.json"));

            Assert.Contains(tempRoot, exception.Message);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DataDirectoryLocator.DataRootEnvironmentVariable,
                null);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static void WarmUpEveryRealRegistryEntry()
    {
        RulesetRegistry.Resolve(RulesetRegistry.CampaignRulesetId);
        ScenarioDefinitionRegistry.Resolve(WatchtowerScenarioContent.ScenarioId);
        ScenarioDefinitionRegistry.Resolve(SunkenChapelScenarioIds.ScenarioId);
        ScenarioDefinitionRegistry.Resolve(HollowMillScenarioIds.ScenarioId);
        CampaignRegistry.Resolve(FrontierCampaignIds.CampaignId);
    }
}
