namespace FiveEGoldBox.Application.Parties;

/// Names the resource a spell slot of a given level is spent from.
///
/// One place that knows the format, so nothing else has to build or parse it.
public static class SpellSlotResources
{
    private const string Prefix = "resource.spell-slot.";

    public static string ForLevel(
        int slotLevel)
    {
        if (slotLevel < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slotLevel),
                slotLevel,
                "Spell slots start at level one.");
        }

        return Prefix + slotLevel.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool IsSpellSlot(
        string resourceId)
    {
        ArgumentNullException.ThrowIfNull(resourceId);

        return resourceId.StartsWith(Prefix, StringComparison.Ordinal);
    }
}
