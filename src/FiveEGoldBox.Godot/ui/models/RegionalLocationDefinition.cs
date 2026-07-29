// Everything the live shell needs to know about one named frontier-region
// location in one place, rather than three separate lookups (position,
// label, exploration scene) that could drift out of sync with each other.
internal sealed record RegionalLocationDefinition(
	string Id,
	string Label,
	RegionalMapPointViewModel Position,
	string ExplorationSceneKey);
