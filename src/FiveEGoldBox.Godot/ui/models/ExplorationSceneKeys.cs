// Centralizes the scene-key strings MockExplorationScenarios and
// ExplorationView both need to agree on, so they can't drift out of sync
// as separate string literals in two files.
internal static class ExplorationSceneKeys
{
	public const string OutpostEntrance = "outpost-entrance";
	public const string BuildingInterior = "building-interior";
	public const string TownStreet = "town-street";
	public const string Cavern = "cavern";
	public const string DungeonCorridor = "dungeon-corridor";
}
