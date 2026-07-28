using FiveEGoldBox.Core.Runtime;

namespace FiveEGoldBox.Application.Combat;

public sealed record CombatSpellAttackOption
{
    internal CombatSpellAttackOption(
        string spellId,
        bool isAvailable,
        EncounterActionUnavailabilityReason unavailabilityReason,
        IReadOnlyList<CombatTargetOption> targets)
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
    }

    public string SpellId { get; }

    public bool IsAvailable { get; }

    /// Why this spell cannot be cast. None when it is available.
    public EncounterActionUnavailabilityReason UnavailabilityReason { get; }

    public IReadOnlyList<CombatTargetOption> Targets { get; }
}
