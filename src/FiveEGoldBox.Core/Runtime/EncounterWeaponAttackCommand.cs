namespace FiveEGoldBox.Core.Runtime;

public sealed record EncounterWeaponAttackCommand
{
    public required long ExpectedRevision { get; init; }

    public required string ActorCombatantId { get; init; }

    public required string TargetCombatantId { get; init; }

    public required string WeaponId { get; init; }

    public required int FirstAttackRoll { get; init; }

    public int? SecondAttackRoll { get; init; }

    public required IReadOnlyList<int>
        DamageRolls
    { get; init; }
}
