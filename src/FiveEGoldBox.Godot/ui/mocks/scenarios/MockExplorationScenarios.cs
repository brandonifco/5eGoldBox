// One method per §10.2 "Exploration" minimum case.
internal static class MockExplorationScenarios
{
	public static ExplorationViewModel Building()
	{
		return new ExplorationViewModel(
			"building-interior",
			"Facing North",
			"N");
	}

	public static ExplorationViewModel TownStreet()
	{
		return new ExplorationViewModel(
			"town-street",
			"Facing East",
			"E");
	}

	public static ExplorationViewModel Cavern()
	{
		return new ExplorationViewModel(
			"cavern",
			"Facing South",
			"S");
	}

	public static ExplorationViewModel DungeonCorridor()
	{
		return new ExplorationViewModel(
			"dungeon-corridor",
			"Facing West",
			"W");
	}

	public static ExplorationViewModel InteractionPrompt()
	{
		return new ExplorationViewModel(
			"dungeon-corridor",
			"Facing North",
			"N",
			new[] { "Press Enter to open the door." });
	}

	public static ExplorationViewModel MovementActive()
	{
		return new ExplorationViewModel(
			"dungeon-corridor",
			"Facing North",
			"N",
			new[] { "ARROWS/NUMPAD TO MOVE — ESC/SPACE TO EXIT" });
	}
}
