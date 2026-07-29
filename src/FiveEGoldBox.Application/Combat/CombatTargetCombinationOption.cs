namespace FiveEGoldBox.Application.Combat;

/// A legal set of targets for a spell that can reach more than one.
public sealed record CombatTargetCombinationOption
{
    internal CombatTargetCombinationOption(
        IReadOnlyList<string> targetCombatantIds)
    {
        ArgumentNullException.ThrowIfNull(targetCombatantIds);

        string[] protectedTargetCombatantIds =
            targetCombatantIds.ToArray();

        if (protectedTargetCombatantIds.Length < 2)
        {
            throw new ArgumentException(
                "A target combination requires at least two targets — a single target is already offered on Targets.",
                nameof(targetCombatantIds));
        }

        TargetCombatantIds =
            Array.AsReadOnly(protectedTargetCombatantIds);
    }

    public IReadOnlyList<string> TargetCombatantIds { get; }
}
