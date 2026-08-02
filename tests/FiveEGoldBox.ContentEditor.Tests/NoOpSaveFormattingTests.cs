using FiveEGoldBox.ContentEditor.Services;

namespace FiveEGoldBox.ContentEditor.Tests;

/// The formatting-fidelity proof the task's own scoping doc calls the
/// riskiest part of this feature: a save that changes nothing must produce
/// a byte-identical file. Runs against a temp copy of the real committed
/// data/rulesets/campaign/core.json -- never the committed file itself.
public sealed class NoOpSaveFormattingTests
{
    [Fact]
    public void ResavingAnUnchangedWeaponProducesAByteIdenticalFile()
    {
        string tempFile = CopyRealRulesetPackToTempFile();

        try
        {
            RulesetContentService service = new(tempFile);
            byte[] before = File.ReadAllBytes(tempFile);

            FiveEGoldBox.Core.Definitions.WeaponDefinition weapon = service.FindWeapon("weapon.longbow")!;
            var result = service.SaveWeapon(weapon);

            Assert.True(result.IsValid, DescribeIssues(result));

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResavingEveryExistingWeaponOneAtATimeProducesAByteIdenticalFile()
    {
        string tempFile = CopyRealRulesetPackToTempFile();

        try
        {
            RulesetContentService service = new(tempFile);
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var weapon in service.LoadWeapons())
            {
                var result = service.SaveWeapon(weapon);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResavingEveryExistingSpellOneAtATimeProducesAByteIdenticalFile()
    {
        string tempFile = CopyRealRulesetPackToTempFile();

        try
        {
            RulesetContentService service = new(tempFile);
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var spell in service.LoadSpells())
            {
                var result = service.SaveSpell(spell);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResavingEveryExistingEquipmentItemOneAtATimeProducesAByteIdenticalFile()
    {
        string tempFile = CopyRealRulesetPackToTempFile();

        try
        {
            RulesetContentService service = new(tempFile);
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var item in service.LoadEquipmentItems())
            {
                var result = service.SaveEquipmentItem(item);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResavingEveryExistingMonsterOneAtATimeProducesAByteIdenticalFile()
    {
        string tempFile = CopyRealRulesetPackToTempFile();

        try
        {
            RulesetContentService service = new(tempFile);
            byte[] before = File.ReadAllBytes(tempFile);

            foreach (var monster in service.LoadMonsters())
            {
                var result = service.SaveMonster(monster);
                Assert.True(result.IsValid, DescribeIssues(result));
            }

            byte[] after = File.ReadAllBytes(tempFile);
            AssertByteIdentical(before, after);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private static void AssertByteIdentical(
        byte[] expected,
        byte[] actual)
    {
        if (expected.AsSpan().SequenceEqual(actual))
        {
            return;
        }

        string expectedText = System.Text.Encoding.UTF8.GetString(expected);
        string actualText = System.Text.Encoding.UTF8.GetString(actual);

        string[] expectedLines = expectedText.Split('\n');
        string[] actualLines = actualText.Split('\n');

        for (int i = 0; i < Math.Min(expectedLines.Length, actualLines.Length); i++)
        {
            if (expectedLines[i] != actualLines[i])
            {
                Assert.Fail(
                    $"First differing line {i + 1}:\nExpected: {expectedLines[i]}\nActual:   {actualLines[i]}");
            }
        }

        Assert.Fail(
            $"Byte content differs (expected {expected.Length} bytes, actual {actual.Length} bytes) but no differing line found within the shorter length.");
    }

    private static string DescribeIssues(
        FiveEGoldBox.Core.Validation.ValidationResult result)
    {
        return string.Join(
            "\n",
            result.Issues.Select(issue => $"{issue.Severity} {issue.Code}: {issue.Message}"));
    }

    internal static string CopyRealRulesetPackToTempFile()
    {
        string realPath = RepositoryLocator.ResolveCoreRulesetPackPath();
        string tempPath = Path.Combine(Path.GetTempPath(), $"core-{Guid.NewGuid():N}.json");
        File.Copy(realPath, tempPath);
        return tempPath;
    }
}
