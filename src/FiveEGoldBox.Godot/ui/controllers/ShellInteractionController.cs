using Godot;

internal sealed class ShellInteractionController : IShellInteractionState
{
	private readonly IShellPresentation _presentationController;
	private readonly IShellCommandBar _commandBarController;

	public ShellInteractionController(
		IShellPresentation presentationController,
		IShellCommandBar commandBarController)
	{
		_presentationController = presentationController;
		_commandBarController = commandBarController;
	}

	public InteractionMode CurrentMode { get; private set; }

	public void ShowExploration()
	{
		CurrentMode = InteractionMode.CommandMenu;
		_presentationController.ShowExploration();
		ShowExplorationCommands();
	}

	public void ShowRegionalMap()
	{
		CurrentMode = InteractionMode.CommandMenu;
		_presentationController.ShowRegionalMap();

		_commandBarController.ShowCommands(
			new CommandDefinition(
				"Travel",
				"[b]T[/b]ravel",
				Key.T,
				() => ReportCommand("Travel")),
			new CommandDefinition(
				"Search",
				"[b]S[/b]earch",
				Key.S,
				() => ReportCommand("Search")),
			new CommandDefinition(
				"Enter",
				"[b]E[/b]nter",
				Key.E,
				() => ReportCommand("Enter")),
			new CommandDefinition(
				"Camp",
				"[b]C[/b]amp",
				Key.C,
				() => ReportCommand("Camp")),
			new CommandDefinition(
				"Inventory",
				"[b]I[/b]nventory",
				Key.I,
				() => ReportCommand("Inventory")),
			new CommandDefinition(
				"Journal",
				"[b]J[/b]ournal",
				Key.J,
				() => ReportCommand("Journal")));
	}

	public void ShowCombat()
	{
		CurrentMode = InteractionMode.CommandMenu;
		_presentationController.ShowCombat();

		_commandBarController.ShowCommands(
			new CommandDefinition(
				"Move",
				"[b]M[/b]ove",
				Key.M,
				() => ReportCommand("Combat movement")),
			new CommandDefinition(
				"Attack",
				"[b]A[/b]ttack",
				Key.A,
				() => ReportCommand("Attack")),
			new CommandDefinition(
				"Cast",
				"[b]C[/b]ast",
				Key.C,
				() => ReportCommand("Cast")),
			new CommandDefinition(
				"Use",
				"[b]U[/b]se",
				Key.U,
				() => ReportCommand("Use")),
			new CommandDefinition(
				"Defend",
				"[b]D[/b]efend",
				Key.D,
				() => ReportCommand("Defend")),
			new CommandDefinition(
				"End Turn",
				"[b]E[/b]nd Turn",
				Key.E,
				() => ReportCommand("End turn")));
	}

	public void ExitExplorationMovementMode()
	{
		CurrentMode = InteractionMode.CommandMenu;

		_presentationController.SetMessage("Movement mode ended.");
		ShowExplorationCommands();
	}

	public void ReportMovement(string movement)
	{
		_presentationController.SetMessage(
			$"{movement}. Backend movement is not connected yet.");
	}

	private void ShowExplorationCommands()
	{
		_commandBarController.ShowCommands(
			new CommandDefinition(
				"Move",
				"[b]M[/b]ove",
				Key.M,
				EnterExplorationMovementMode),
			new CommandDefinition(
				"View",
				"[b]V[/b]iew",
				Key.V,
				() => ReportCommand("View")),
			new CommandDefinition(
				"Cast",
				"[b]C[/b]ast",
				Key.C,
				() => ReportCommand("Cast")),
			new CommandDefinition(
				"Area",
				"[b]A[/b]rea",
				Key.A,
				() => ReportCommand("Area")),
			new CommandDefinition(
				"Encamp",
				"[b]E[/b]ncamp",
				Key.E,
				() => ReportCommand("Encamp")),
			new CommandDefinition(
				"Search",
				"[b]S[/b]earch",
				Key.S,
				() => ReportCommand("Search")),
			new CommandDefinition(
				"Look",
				"[b]L[/b]ook",
				Key.L,
				() => ReportCommand("Look")));
	}

	private void EnterExplorationMovementMode()
	{
		CurrentMode = InteractionMode.ExplorationMovement;

		_commandBarController.ShowMovementPrompt();
		_presentationController.SetMessage(
			"Movement active: arrows or numpad 8/2 move; " +
			"4/6 turn. Press Esc or Space to return.");
	}

	private void ReportCommand(string command)
	{
		_presentationController.SetMessage(
			$"{command} selected. Backend behavior is not connected yet.");
	}
}
