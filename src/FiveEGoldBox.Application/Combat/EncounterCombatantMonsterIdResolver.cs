using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

/// The opposition's monster type ID, keyed by combatant ID -- the same
/// "resolve once, from the scenario the encounter names" shape
/// EncounterCombatantDisplayNameResolver already established for names.
///
/// A live encounter's own CombatantId is per-placement (e.g.
/// "combatant.mill-rat.first"/"combatant.mill-rat.second" for two rats from
/// the same monster), not the shared monster type ("monster.mill-rat") a
/// client would need to key art or other monster-type-level presentation
/// off of -- that type ID lives only on the scenario's own
/// EncounterCombatantDefinition placement, internal to Application and
/// unreachable from a client asking CombatOperations for a read-only
/// projection, same boundary DisplayName's own resolver already crosses.
internal static class EncounterCombatantMonsterIdResolver
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

        Dictionary<string, string> monsterIds = new(StringComparer.Ordinal);

        foreach (EncounterCombatantDefinition placement
            in encounterDefinition.Combatants)
        {
            monsterIds[placement.CombatantId] = placement.MonsterId;
        }

        return monsterIds;
    }
}
