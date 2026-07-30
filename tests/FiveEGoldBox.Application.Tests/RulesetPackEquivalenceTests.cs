using System.Collections;
using System.Reflection;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Core.Definitions;

namespace FiveEGoldBox.Application.Tests;

/// Proves data/rulesets/campaign/core.json is a byte-for-byte equivalent
/// rendering of CampaignRulesetContent.CreateRulesetDefinition()'s hardcoded
/// output. This is what makes deleting the hardcoded C# safe (Phase 1c of
/// docs/2026-07-30-data-driven-content-plan.md) -- the same discipline this
/// codebase already applies to the frozen combat transcripts.
public sealed class RulesetPackEquivalenceTests
{
    [Fact]
    public void CoreRulesetPack_MatchesCampaignRulesetContent()
    {
        string packPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "data-rulesets-campaign-core.json");

        RulesetDefinition fromPack = RulesetPackLoader.Parse(
            [File.ReadAllText(packPath)]);

        RulesetDefinition fromHardcodedContent =
            CampaignRulesetContent.CreateRulesetDefinition();

        AssertDeepEqual(
            fromHardcodedContent,
            fromPack,
            "RulesetDefinition");
    }

    /// A generic structural comparer, not a type-specific one, because the
    /// point is proving every field of every nested definition record
    /// matches -- writing that assertion by hand once per type is exactly
    /// the kind of duplication that silently drifts out of sync with the
    /// definitions it's supposed to be checking.
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

        // Collections are compared structurally regardless of concrete type:
        // a hand-written collection expression and a LINQ .ToArray() can
        // both legitimately produce IReadOnlyList<T>, just via different
        // compiler-generated or BCL types, and that difference carries no
        // meaning here.
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
