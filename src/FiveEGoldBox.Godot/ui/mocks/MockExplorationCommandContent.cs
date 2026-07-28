// The exploration command set as data, not hardcoded CommandDefinition
// construction (M6c). Distinct from MockScenarioCatalog — that's the QA/
// stress-test catalog ("make no claim about backend rule behavior");
// this is the actual pre-integration content driving the live exploration
// screen, and it's what a real backend response would eventually replace.
internal static class MockExplorationCommandContent
{
	public static CommandSetViewModel Current()
	{
		return new CommandSetViewModel(
			new[]
			{
				new CommandViewModel("move", "Move", "M"),
				new CommandViewModel("view", "View", "V"),
				new CommandViewModel("cast", "Cast", "C"),
				new CommandViewModel("area", "Area", "A"),
				new CommandViewModel("encamp", "Encamp", "E"),
				new CommandViewModel("search", "Search", "S"),
				new CommandViewModel("look", "Look", "L"),
			},
			ShellInteractionContext.CommandMenu);
	}
}
