using System;
using System.Collections.Generic;
using Godot;

internal sealed class ShellInteractionController : IShellInteractionState
{
	private readonly IShellPresentation _presentationController;
	private readonly IShellCommandBar _commandBarController;
	private readonly Stack<ShellInteractionContext> _contextStack = new();

	public ShellInteractionController(
		IShellPresentation presentationController,
		IShellCommandBar commandBarController)
	{
		_presentationController = presentationController;
		_commandBarController = commandBarController;
		_contextStack.Push(ShellInteractionContext.CommandMenu);
	}

	public ShellInteractionContext CurrentContext => _contextStack.Peek();

	public void ShowExploration()
	{
		ResetContext();
		_presentationController.ShowExploration();
		ShowExplorationCommands();
	}

	public void ShowRegionalMap()
	{
		ResetContext();
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
		ResetContext();
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
		PopContext(ShellInteractionContext.DirectMovement);

		_presentationController.SetMessage("Movement mode ended.");
		ShowExplorationCommands();
	}

	public void ReportMovement(string movement)
	{
		_presentationController.SetMessage(
			$"{movement}. Backend movement is not connected yet.");
	}

	// M6c: driven from CommandSetViewModel data instead of hardcoded
	// CommandDefinition construction — RegionalMap/Combat above stay
	// hardcoded on purpose, that's M7/M8's own milestone to do the same.
	private void ShowExplorationCommands()
	{
		CommandSetViewModel commandSet = MockExplorationCommandContent.Current();
		Dictionary<string, Action> handlers =
			BuildExplorationCommandHandlers();
		List<CommandDefinition> commands = new();

		foreach (CommandViewModel commandViewModel in commandSet.Commands)
		{
			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				handlers[commandViewModel.CommandId]));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	private Dictionary<string, Action> BuildExplorationCommandHandlers()
	{
		return new Dictionary<string, Action>
		{
			["move"] = EnterExplorationMovementMode,
			["view"] = () => ReportCommand("View"),
			["cast"] = () => ReportCommand("Cast"),
			["area"] = () => ReportCommand("Area"),
			["encamp"] = () => ReportCommand("Encamp"),
			["search"] = () => ReportCommand("Search"),
			["look"] = () => ReportCommand("Look"),
		};
	}

	private void EnterExplorationMovementMode()
	{
		PushContext(ShellInteractionContext.DirectMovement);

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

	private void ResetContext()
	{
		_contextStack.Clear();
		_contextStack.Push(ShellInteractionContext.CommandMenu);
	}

	// Cancel priority is Godot's own unhandled-input order (deepest node
	// first, SetInputAsHandled stops it going further) — a modal already
	// wins over ShellInputRouter's exploration-movement Escape handling
	// for that reason alone. This stack exists so CurrentContext reports
	// the same priority explicitly: whoever captures a cancel key this
	// way must Push/Pop their context here too, or CurrentContext goes
	// stale while they hold input.
	private void PushContext(ShellInteractionContext context)
	{
		_contextStack.Push(context);
	}

	private void PopContext(ShellInteractionContext expected)
	{
		if (CurrentContext == expected && _contextStack.Count > 1)
		{
			_contextStack.Pop();
		}
	}
}
