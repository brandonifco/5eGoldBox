using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// Real display names for the opposition a scenario places on an
/// encounter's battlefield, keyed by combatant ID -- the same "resolve once,
/// from the scenario/ruleset the encounter names" shape
/// EncounterPartySideResolver already established for whose turn it is.
///
/// A party member already carries its own name (PartyMemberState.
/// DisplayName); this exists for the opposition, whose name lives on the
/// ruleset's MonsterDefinition and is reached only through the scenario's
/// own EncounterCombatantDefinition placement -- both internal to
/// Application, and neither reachable from a client asking CombatOperations
/// for a read-only projection.
internal static class EncounterCombatantDisplayNameResolver
{
    internal static IReadOnlyDictionary<string, string> Resolve(
        ApplicationSessionState session,
        EncounterState encounter)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(encounter);

        ScenarioDefinition scenario =
            ScenarioDefinitionRegistry.Resolve(session);

        EncounterDefinition encounterDefinition = scenario.Encounters
            .FirstOrDefault(candidate => string.Equals(
                candidate.EncounterId,
                encounter.EncounterId,
                StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Encounter '{encounter.EncounterId}' is not part of scenario '{scenario.ScenarioId}'.");

        ValidatedRuleset ruleset =
            RulesetRegistry.Resolve(scenario.RulesetId);

        Dictionary<string, string> names = new(StringComparer.Ordinal);

        foreach (EncounterCombatantDefinition placement
            in encounterDefinition.Combatants)
        {
            MonsterDefinition monster = ruleset.Definition.Monsters
                .FirstOrDefault(candidate => string.Equals(
                    candidate.Id,
                    placement.MonsterId,
                    StringComparison.Ordinal))
                ?? throw new InvalidOperationException(
                    $"Combatant '{placement.CombatantId}' references monster '{placement.MonsterId}', which the ruleset does not define.");

            names[placement.CombatantId] = monster.Name;
        }

        return names;
    }
}
