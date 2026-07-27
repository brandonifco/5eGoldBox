using FiveEGoldBox.Core.Rules;

namespace FiveEGoldBox.Application.Parties;

/// Names the resource a spell slot of a given level is spent from.
///
/// The format moved to `RuleIds.Resources` when Core gained a reason to build
/// one of these itself — a resolved spell carries the slot it spends. This
/// stays as the name the application layer already calls it, and delegates,
/// so there is one format rather than two that agree by coincidence.
public static class SpellSlotResources
{
    public static string ForLevel(
        int slotLevel)
    {
        return RuleIds.Resources.SpellSlot(slotLevel);
    }

    public static bool IsSpellSlot(
        string resourceId)
    {
        return RuleIds.Resources.IsSpellSlot(resourceId);
    }
}
