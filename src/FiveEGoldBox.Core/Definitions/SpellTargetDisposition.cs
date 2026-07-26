namespace FiveEGoldBox.Core.Definitions;

/// Who a spell may be cast at.
///
/// Stated rather than inferred. An earlier version worked this out from
/// whether the spell healed, which is right for Cure Wounds and Fire Bolt and
/// wrong for every buff: Bless heals nobody and is plainly not cast at the
/// enemy. There is no mechanical property that separates a blessing from a
/// curse, so the spell says.
public enum SpellTargetDisposition
{
    Allies,
    Enemies,
    Any
}
