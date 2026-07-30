using System.Collections.Concurrent;
using FiveEGoldBox.Application.Content;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Validation;

namespace FiveEGoldBox.Application.Scenarios;

/// Finds the content for whatever scenario a session is running.
///
/// This is the seam that lets execution stop naming Watchtower: a session
/// already carries a generic ScenarioId, so rules can resolve their content
/// from it rather than reaching for a constant. Adding a second scenario means
/// registering it here, not editing the rules.
///
/// Definitions are validated the first time they are resolved and cached
/// afterwards, so content errors surface immediately rather than midway through
/// a session, and validation is not repaid on every rule call.
internal static class ScenarioDefinitionRegistry
{
    private static readonly IReadOnlyDictionary<string, Func<ScenarioDefinition>>
        Factories = new Dictionary<string, Func<ScenarioDefinition>>(
            StringComparer.Ordinal)
        {
            [WatchtowerScenarioContent.ScenarioId] = LoadWatchtowerScenario,
            [SunkenChapelScenarioIds.ScenarioId] = LoadSunkenChapelScenario,
            [HollowMillScenarioIds.ScenarioId] = LoadHollowMillScenario
        };

    private static readonly ConcurrentDictionary<string, ScenarioDefinition>
        Cache = new(StringComparer.Ordinal);

    internal static ScenarioDefinition Resolve(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return Resolve(session.ScenarioId);
    }

    internal static ScenarioDefinition Resolve(
        string scenarioId)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
        {
            throw new ArgumentException(
                "Scenario ID is required.",
                nameof(scenarioId));
        }

        return Cache.GetOrAdd(scenarioId, CreateValidated);
    }

    internal static bool IsRegistered(
        string scenarioId)
    {
        return Factories.ContainsKey(scenarioId);
    }

    private static ScenarioDefinition CreateValidated(
        string scenarioId)
    {
        if (!Factories.TryGetValue(
            scenarioId,
            out Func<ScenarioDefinition>? factory))
        {
            throw new ArgumentException(
                $"No scenario is registered under '{scenarioId}'.",
                nameof(scenarioId));
        }

        ScenarioDefinition definition = factory();
        ValidationResult validation =
            ScenarioDefinitionValidator.Validate(definition);

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Scenario '{scenarioId}' has invalid content: "
                    + string.Join(
                        "; ",
                        validation.Issues
                            .Where(issue =>
                                issue.Severity == ValidationSeverity.Error)
                            .Select(issue => issue.Message)));
        }

        return definition;
    }

    private static ScenarioDefinition LoadWatchtowerScenario()
    {
        return LoadScenario("watchtower");
    }

    private static ScenarioDefinition LoadSunkenChapelScenario()
    {
        return LoadScenario("sunken-chapel");
    }

    private static ScenarioDefinition LoadHollowMillScenario()
    {
        return LoadScenario("hollow-mill");
    }

    private static ScenarioDefinition LoadScenario(
        string scenarioDirectoryName)
    {
        string packPath = DataDirectoryLocator.ResolveDataFilePath(
            Path.Combine("scenarios", scenarioDirectoryName, "scenario.json"));

        return ScenarioPackLoader.Load(packPath);
    }
}
