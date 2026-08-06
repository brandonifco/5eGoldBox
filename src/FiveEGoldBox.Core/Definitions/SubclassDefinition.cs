namespace FiveEGoldBox.Core.Definitions;

/// A class's own sub-choice -- identity only, deliberately no mechanical
/// fields yet. Real 5e subclasses grant their first features above level 1
/// for three of this ruleset's four classes (Cleric picks at 1st level;
/// Wizard at 2nd; Fighter and Rogue at 3rd), and this engine has no leveling
/// system at all -- so a feature grant here would describe something that
/// can never actually happen yet. `ClassDefinition.Subclasses` names these;
/// mechanical content joins whenever leveling does.
public sealed record SubclassDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }
}
