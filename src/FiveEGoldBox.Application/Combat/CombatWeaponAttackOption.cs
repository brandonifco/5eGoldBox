namespace FiveEGoldBox.Application.Combat;

public sealed record CombatWeaponAttackOption
{
    internal CombatWeaponAttackOption(
        string weaponId,
        bool isAvailable,
        IReadOnlyList<CombatTargetOption> targets)
    {
        if (string.IsNullOrWhiteSpace(weaponId))
        {
            throw new ArgumentException(
                "Weapon ID is required.",
                nameof(weaponId));
        }

        ArgumentNullException.ThrowIfNull(targets);

        CombatTargetOption[] protectedTargets = targets.ToArray();

        if (isAvailable != protectedTargets.Any(target => target.IsAvailable))
        {
            throw new ArgumentException(
                "Weapon availability must match its target options.",
                nameof(targets));
        }

        WeaponId = weaponId;
        IsAvailable = isAvailable;
        Targets = Array.AsReadOnly(protectedTargets);
    }

    public string WeaponId { get; }

    public bool IsAvailable { get; }

    public IReadOnlyList<CombatTargetOption> Targets { get; }
}
