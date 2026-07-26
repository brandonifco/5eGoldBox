namespace FiveEGoldBox.Application.Parties;

/// Something a character spends and gets back on a rest.
///
/// Spell slots are the first, and the shape is deliberately not spell-shaped:
/// Second Wind uses, Rage uses and Channel Divinity are the same thing with a
/// different identifier, so they need no second mechanism.
///
/// A slot's level lives in its identifier rather than in a field here. That is
/// enough while the campaign is level one and every slot is first-level. Real
/// upcasting — spending any slot of at least the spell's level — wants the
/// level as data, and is the point at which this record earns one.
public sealed record CharacterResourceState
{
    public required string ResourceId { get; init; }

    public required int Remaining { get; init; }

    public required int Maximum { get; init; }
}
