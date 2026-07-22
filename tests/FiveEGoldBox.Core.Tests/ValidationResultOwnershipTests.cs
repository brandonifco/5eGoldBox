using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Core.Tests;

public sealed class ValidationResultOwnershipTests
{
    [Fact]
    public void Constructor_ProtectsSourceIssuesAndIssueOrder()
    {
        ValidationIssue warning = new(
            ValidationSeverity.Warning,
            "warning.first",
            "First warning.");
        ValidationIssue error = new(
            ValidationSeverity.Error,
            "error.second",
            "Second error.");
        List<ValidationIssue> sourceIssues = [warning, error];

        ValidationResult result = new(sourceIssues);

        sourceIssues.Clear();

        Assert.Empty(sourceIssues);
        Assert.Equal([warning, error], result.Issues);
        Assert.False(result.IsValid);
        Assert.False(result.Issues is ValidationIssue[]);
        Assert.False(result.Issues is List<ValidationIssue>);

        IList<ValidationIssue> mutableIssues =
            Assert.IsAssignableFrom<IList<ValidationIssue>>(result.Issues);

        Assert.Throws<NotSupportedException>(() => mutableIssues.RemoveAt(0));
        Assert.Equal([warning, error], result.Issues);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Success_ExposesProtectedEmptyIssueCollection()
    {
        ValidationResult result = ValidationResult.Success;

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
        Assert.False(result.Issues is ValidationIssue[]);
        Assert.False(result.Issues is List<ValidationIssue>);

        IList<ValidationIssue> mutableIssues =
            Assert.IsAssignableFrom<IList<ValidationIssue>>(result.Issues);

        Assert.Throws<NotSupportedException>(() =>
            mutableIssues.Add(new ValidationIssue(
                ValidationSeverity.Error,
                "error.added",
                "Added error.")));
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void RulesetValidator_ReturnsProtectedIssueCollection()
    {
        RulesetDefinition ruleset = new()
        {
            Id = "",
            Name = ""
        };

        ValidationResult result = RulesetValidator.Validate(ruleset);
        string[] originalCodes = result.Issues.Select(issue => issue.Code).ToArray();

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Issues);

        IList<ValidationIssue> mutableIssues =
            Assert.IsAssignableFrom<IList<ValidationIssue>>(result.Issues);

        Assert.Throws<NotSupportedException>(() => mutableIssues.Clear());
        Assert.Equal(originalCodes, result.Issues.Select(issue => issue.Code));
        Assert.False(result.IsValid);
    }
}
