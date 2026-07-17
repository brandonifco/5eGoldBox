using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterDeathSavingThrowCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required D20RollMode RollMode { get; init; }

    public required int FirstRoll { get; init; }

    public int? SecondRoll { get; init; }

    public required int SavingThrowBonus { get; init; }
}
