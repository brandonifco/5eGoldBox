// The regional-map command set as data (M7d), mirroring
// MockExplorationCommandContent's role (M6c) — the actual
// pre-integration content driving the live regional-map screen, not the
// QA/stress-test catalog.
internal static class MockRegionalMapCommandContent
{
	public static CommandSetViewModel Current()
	{
		return new CommandSetViewModel(
			new[]
			{
				new CommandViewModel("travel", "Travel", "T"),
				new CommandViewModel("search", "Search", "S"),
				new CommandViewModel("enter", "Enter", "E"),
				new CommandViewModel("camp", "Camp", "C"),
				new CommandViewModel("inventory", "Inventory", "I"),
				new CommandViewModel("journal", "Journal", "J"),
				new CommandViewModel("options", "Options", "O"),
			},
			ShellInteractionContext.SelectionList);
	}
}
