using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Combat;

public static class CombatOperations
{
    public static CombatView Query(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        ApplicationSessionState canonical =
            ApplicationSessionRules.CreateCanonical(session);

        if (canonical.ActiveEncounter is null)
        {
            throw new InvalidOperationException(
                "The current application session does not contain an active encounter.");
        }

        HashSet<string> controlledCombatantIds =
            canonical.Party.Members
                .Select(member => member.PartyMemberId)
                .ToHashSet(StringComparer.Ordinal);

        return CombatViewFactory.Create(
            canonical.ActiveEncounter.Encounter,
            controlledCombatantIds);
    }
}
