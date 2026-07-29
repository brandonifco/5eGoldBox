// The combat command set as data (M8d), mirroring
// MockExplorationCommandContent/MockRegionalMapCommandContent's role —
// the actual pre-integration content driving the live combat screen.
internal static class MockCombatCommandContent
{
	public static CommandSetViewModel Current()
	{
		return new CommandSetViewModel(
			new[]
			{
				new CommandViewModel("move", "Move", "M"),
				new CommandViewModel("attack", "Attack", "A"),
				new CommandViewModel("cast", "Cast", "C"),
				new CommandViewModel("use", "Use", "U"),
				new CommandViewModel("defend", "Defend", "D"),
				new CommandViewModel("end-turn", "End Turn", "E"),
			},
			ShellInteractionContext.CommandMenu);
	}
}
