using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Randomness;

/// Loads a ruleset for play, which means more than loading it for correctness.
///
/// Core validates that a ruleset is well-formed. It cannot validate that this
/// layer can actually roll the dice in it, because Core does not know this
/// layer exists. So the two checks are separate and this is where they meet:
/// content that Core is happy with but Application cannot resolve is refused
/// here, at load, rather than partway through a fight.
///
/// The check matters most for dice Core gains later. Every die Core defines
/// today is rollable; the point is that the next one will fail here until
/// somebody makes it rollable.
internal static class ApplicationRulesetLoader
{
    /// The convenience every caller that isn't inspecting validation issues
    /// one by one actually wants: load it, and if it's not good enough to
    /// play, say why in one exception rather than making every caller
    /// re-render the issue list itself.
    internal static ValidatedRuleset LoadOrThrow(
        RulesetDefinition definition)
    {
        RulesetLoadResult load = Load(definition);

        if (!load.IsValid || load.Ruleset is null)
        {
            throw new InvalidOperationException(
                "The ruleset could not be validated: "
                    + string.Join(
                        "; ",
                        load.Validation.Issues
                            .Where(issue =>
                                issue.Severity == ValidationSeverity.Error)
                            .Select(issue => $"{issue.Code} {issue.Message}")));
        }

        return load.Ruleset;
    }

    internal static RulesetLoadResult Load(
        RulesetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        RulesetLoadResult load = ValidatedRuleset.Load(definition);

        if (!load.IsValid || load.Ruleset is null)
        {
            return load;
        }

        List<ValidationIssue> issues = [];

        foreach (WeaponDefinition weapon in definition.Weapons)
        {
            AddUnrollableDieIssue(
                issues,
                weapon.Damage.Die,
                weapon.Id,
                "damage");

            if (weapon.VersatileDamage is not null)
            {
                AddUnrollableDieIssue(
                    issues,
                    weapon.VersatileDamage.Die,
                    weapon.Id,
                    "versatile damage");
            }
        }

        return issues.Count == 0
            ? load
            : RulesetLoadResult.Failure(new ValidationResult(issues));
    }

    private static void AddUnrollableDieIssue(
        List<ValidationIssue> issues,
        DieType die,
        string weaponId,
        string role)
    {
        if (ApplicationRandomSequence.IsSupported(die))
        {
            return;
        }

        issues.Add(new ValidationIssue(
            ValidationSeverity.Error,
            "ruleset.weapons.damage_die.unrollable",
            $"Ruleset weapon '{weaponId}' rolls {role} die '{die}', which the deterministic random sequence cannot produce."));
    }
}
