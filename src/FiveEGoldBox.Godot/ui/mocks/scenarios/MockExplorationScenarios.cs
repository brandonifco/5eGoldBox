// One method per §10.2 "Exploration" minimum case.
internal static class MockExplorationScenarios
{
	public static ExplorationViewModel Building()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.BuildingInterior,
			"Facing North",
			"N");
	}

	public static ExplorationViewModel TownStreet()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.TownStreet,
			"Facing East",
			"E");
	}

	public static ExplorationViewModel Cavern()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.Cavern,
			"Facing South",
			"S");
	}

	public static ExplorationViewModel DungeonCorridor()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.DungeonCorridor,
			"Facing West",
			"W");
	}

	public static ExplorationViewModel InteractionPrompt()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.DungeonCorridor,
			"Facing North",
			"N",
			new[] { "Press Enter to open the door." });
	}

	public static ExplorationViewModel MovementActive()
	{
		return new ExplorationViewModel(
			ExplorationSceneKeys.DungeonCorridor,
			"Facing North",
			"N",
			new[] { "ARROWS/NUMPAD TO MOVE — ESC/SPACE TO EXIT" });
	}
}
