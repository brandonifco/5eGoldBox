using FiveEGoldBox.Core.Internal;

namespace FiveEGoldBox.Core.Validation;

public sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public IReadOnlyList<ValidationIssue> Issues { get; init; } =
        CoreCollectionProtection.ProtectList(Issues);

    public bool IsValid => Issues.All(issue => issue.Severity != ValidationSeverity.Error);

    public static ValidationResult Success { get; } = new(Array.Empty<ValidationIssue>());
}
