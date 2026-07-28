using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatSpellAttackOption
{
    internal CombatSpellAttackOption(
        string spellId,
        bool isAvailable,
        EncounterActionUnavailabilityReason unavailabilityReason,
        IReadOnlyList<CombatTargetOption> targets,
        IReadOnlyList<CombatTargetCombinationOption>? targetCombinations
            = null)
    {
        if (string.IsNullOrWhiteSpace(spellId))
        {
            throw new ArgumentException(
                "Spell ID is required.",
                nameof(spellId));
        }

        ArgumentNullException.ThrowIfNull(targets);

        CombatTargetOption[] protectedTargets = targets.ToArray();

        if (isAvailable != protectedTargets.Any(target => target.IsAvailable))
        {
            throw new ArgumentException(
                "Spell availability must match its target options.",
                nameof(targets));
        }

        SpellId = spellId;
        IsAvailable = isAvailable;
        UnavailabilityReason = unavailabilityReason;
        Targets = Array.AsReadOnly(protectedTargets);
        TargetCombinations = targetCombinations is null
            ? Array.Empty<CombatTargetCombinationOption>()
            : Array.AsReadOnly(targetCombinations.ToArray());
    }

    public string SpellId { get; }

    public bool IsAvailable { get; }

    /// Why this spell cannot be cast. None when it is available.
    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }

    public IReadOnlyList<CombatTargetOption> Targets { get; }

    /// Legal sets of two or more targets, for a spell that can reach more
    /// than one. Empty for a spell that only ever names one.
    public IReadOnlyList<CombatTargetCombinationOption>
        TargetCombinations
    { get; }
}
