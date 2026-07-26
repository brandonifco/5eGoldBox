using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

/// A participant ready to be placed, plus the bonus it rolls initiative with.
internal readonly record struct ScenarioEncounterCombatant(
    EncounterParticipantSetup Setup,
    int InitiativeBonus);
