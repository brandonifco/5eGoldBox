using System.Collections;
using System.Reflection;
using FiveEGoldBox.Application.Campaigns;
using FiveEGoldBox.Application.Content;

namespace FiveEGoldBox.Application.Tests;

/// Proves data/campaigns/frontier/campaign.json is a byte-for-byte
/// equivalent rendering of FrontierCampaignContent.CreateDefinition()'s
/// hardcoded output. This is what makes deleting the hardcoded roster safe
/// (Phase 3c of docs/2026-07-30-data-driven-content-plan.md).
public sealed class CampaignPackEquivalenceTests
{
    [Fact]
    public void FrontierCampaignPack_MatchesFrontierCampaignContent()
    {
        string packPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "data-campaigns-frontier-campaign.json");

        CampaignDefinition fromPack = CampaignPackLoader.Load(packPath);

        CampaignDefinition fromHardcodedContent =
            FrontierCampaignContent.CreateDefinition();

        AssertDeepEqual(fromHardcodedContent, fromPack, "CampaignDefinition");
    }

    /// Same generic structural comparer RulesetPackEquivalenceTests and
    /// ScenarioPackEquivalenceTests used.
    private static void AssertDeepEqual(
        object? expected,
        object? actual,
        string path)
    {
        if (expected is null || actual is null)
        {
            Assert.True(
                expected is null && actual is null,
                $"{path}: expected '{expected}' but got '{actual}'");
            return;
        }

        if (expected is IDictionary expectedDictionary)
        {
            IDictionary actualDictionary = (IDictionary)actual;

            Assert.True(
                expectedDictionary.Count == actualDictionary.Count,
                $"{path}: dictionary count mismatch, expected {expectedDictionary.Count} but got {actualDictionary.Count}");

            foreach (object key in expectedDictionary.Keys)
            {
                Assert.True(
                    actualDictionary.Contains(key),
                    $"{path}: missing key '{key}'");

                AssertDeepEqual(
                    expectedDictionary[key],
                    actualDictionary[key],
                    $"{path}[{key}]");
            }

            return;
        }

        if (expected is IEnumerable expectedEnumerable
            && expected is not string)
        {
            List<object> expectedItems = expectedEnumerable
                .Cast<object>()
                .ToList();
            List<object> actualItems = ((IEnumerable)actual)
                .Cast<object>()
                .ToList();

            Assert.True(
                expectedItems.Count == actualItems.Count,
                $"{path}: list count mismatch, expected {expectedItems.Count} but got {actualItems.Count}");

            for (int index = 0; index < expectedItems.Count; index++)
            {
                AssertDeepEqual(
                    expectedItems[index],
                    actualItems[index],
                    $"{path}[{index}]");
            }

            return;
        }

        Type type = expected.GetType();

        Assert.True(
            type == actual.GetType(),
            $"{path}: type mismatch, expected {type} but got {actual.GetType()}");

        if (type.IsPrimitive
            || type == typeof(string)
            || type == typeof(decimal)
            || type.IsEnum)
        {
            Assert.True(
                Equals(expected, actual),
                $"{path}: expected '{expected}' but got '{actual}'");
            return;
        }

        foreach (PropertyInfo property in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
        {
            AssertDeepEqual(
                property.GetValue(expected),
                property.GetValue(actual),
                $"{path}.{property.Name}");
        }
    }
}
