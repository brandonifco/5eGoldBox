namespace FiveEGoldBox.Core.Runtime;

/// What ends an effect early, however it happens: a new concentration effect
/// replacing it, a failed concentration check, or (see
/// EncounterTurnAdvancementRules) its rounds running out. All three need the
/// same two things done — drop every copy of it, wherever it landed, and stop
/// crediting the source with concentrating on it.
internal static class ActiveEffectRules
{
    /// Removes every copy of an effect and clears the source's concentration
    /// if this was what it was sustaining.
    internal static EncounterParticipantState[] Drop(
        EncounterParticipantState[] participants,
        string sourceCombatantId,
        string effectId)
    {
        for (int index = 0; index < participants.Length; index++)
        {
            EncounterParticipantState participant =
                participants[index];

            participants[index] = participant with
            {
                ActiveEffects = participant.ActiveEffects
                    .Where(effect => !Matches(
                        effect,
                        sourceCombatantId,
                        effectId))
                    .ToArray()
            };
        }

        return ClearConcentrationIfMatching(
            participants,
            sourceCombatantId,
            effectId);
    }

    /// Clears the source's concentration if it was on this effect. Split out
    /// from Drop because a natural expiry (EncounterTurnAdvancementRules)
    /// already removes its own copy while decrementing every effect in the
    /// same pass, and only needs this half.
    internal static EncounterParticipantState[]
        ClearConcentrationIfMatching(
            EncounterParticipantState[] participants,
            string sourceCombatantId,
            string effectId)
    {
        int sourceIndex = Array.FindIndex(
            participants,
            candidate => string.Equals(
                candidate.Combatant.CombatantId,
                sourceCombatantId,
                StringComparison.Ordinal));

        if (sourceIndex >= 0
            && string.Equals(
                participants[sourceIndex]
                    .ConcentratingOnEffectId,
                effectId,
                StringComparison.Ordinal))
        {
            participants[sourceIndex] =
                participants[sourceIndex] with
                {
                    ConcentratingOnEffectId = null
                };
        }

        return participants;
    }

    private static bool Matches(
        ActiveEffect effect,
        string sourceCombatantId,
        string effectId)
    {
        return string.Equals(
                effect.EffectId,
                effectId,
                StringComparison.Ordinal)
            && string.Equals(
                effect.SourceCombatantId,
                sourceCombatantId,
                StringComparison.Ordinal);
    }
}
