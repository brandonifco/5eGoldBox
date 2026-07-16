using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterState
{
    public required string EncounterId { get; init; }

    public required long Revision { get; init; }

    public required EncounterBattlefieldState
        Battlefield { get; init; }

    public required IReadOnlyList<EncounterParticipantState>
        Participants { get; init; }

    public required CombatTurnState TurnState { get; init; }

    public required EncounterLifecycleState LifecycleState { get; init; }

    public IReadOnlyList<InitiativeOrderEntry> InitiativeOrder =>
        TurnState.InitiativeOrder;

    public string ActiveCombatantId =>
        CombatTurnRules.GetActiveCombatant(
            TurnState)
        .CombatantId;
}
