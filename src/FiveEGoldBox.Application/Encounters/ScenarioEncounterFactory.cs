using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Application.Randomness;
using FiveEGoldBox.Application.Scenarios.Definitions;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Encounters;

/// Starts the fight a scenario authored.
///
/// Everything here comes from the encounter's own definition - the ground it
/// is fought on, who is standing on it and where. Which scenario that is makes
/// no difference, so any adventure can have its own battle rather than
/// borrowing the first one's.
///
/// What happens once the fight starts is a separate matter: enemy tactics are
/// still written per adventure rather than authored as data.
internal static class ScenarioEncounterFactory
{
    internal static EncounterState Create(
        EncounterDefinition definition,
        PartyState party,
        ValidatedRuleset ruleset,
        int randomSeed,
        int randomValuesConsumed,
        out int updatedRandomValuesConsumed)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(ruleset);

        if (party.Members.Count > definition.PartyStartingPositions.Count)
        {
            throw new InvalidOperationException(
                $"Encounter '{definition.EncounterId}' places {definition.PartyStartingPositions.Count} party members, but the party has {party.Members.Count}.");
        }

        List<EncounterParticipantSetup> participants = [];
        List<int> initiativeBonuses = [];

        for (int index = 0; index < party.Members.Count; index++)
        {
            PartyEncounterParticipant participant =
                PartyEncounterMapper.CreateParticipant(
                    party.Members[index],
                    ruleset,
                    definition.PartySideId,
                    definition.PartyStartingPositions[index]);

            participants.Add(participant.Setup);
            initiativeBonuses.Add(participant.InitiativeBonus);
        }

        foreach (CombatantDefinition combatant in definition.Combatants)
        {
            ScenarioEncounterCombatant opponent =
                ScenarioCombatantMapper.CreateParticipant(
                    combatant,
                    ruleset);

            participants.Add(opponent.Setup);
            initiativeBonuses.Add(opponent.InitiativeBonus);
        }

        // One roll per participant, in placement order, so a scenario with a
        // different number of combatants consumes exactly what it needs.
        IReadOnlyList<int> initiativeRolls =
            ApplicationRandomSequence.GenerateD20Rolls(
                randomSeed,
                randomValuesConsumed,
                participants.Count,
                out updatedRandomValuesConsumed);

        InitiativeOrderCombatant[] initiativeCombatants = participants
            .Select((participant, index) => new InitiativeOrderCombatant
            {
                CombatantId = participant.Combatant.CombatantId,
                Initiative = InitiativeRules.ResolveInitiative(
                    D20RollMode.Normal,
                    initiativeRolls[index],
                    secondRoll: null,
                    initiativeBonus: initiativeBonuses[index])
            })
            .ToArray();

        return EncounterRules.Start(
            definition.EncounterId,
            CreateBattlefield(definition),
            participants,
            InitiativeOrderRules.ResolveOrder(initiativeCombatants));
    }

    private static EncounterBattlefieldState CreateBattlefield(
        EncounterDefinition definition)
    {
        return new EncounterBattlefieldState
        {
            BattlefieldId = definition.BattlefieldId,
            Width = definition.Width,
            Height = definition.Height,
            BlockedPositions = definition.BlockedPositions,
            CoverPositions = Array.Empty<EncounterCoverPosition>(),
            DifficultTerrainPositions = Array.Empty<GridPosition>()
        };
    }
}
