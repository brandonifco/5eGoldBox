using System;
using System.Collections.Generic;
using Godot;

internal sealed class ShellInteractionController : IShellInteractionState
{
	private readonly IShellPresentation _presentationController;
	private readonly IShellCommandBar _commandBarController;
	private readonly IShellConfirmation _confirmation;
	private readonly Stack<ShellInteractionContext> _contextStack = new();

	public ShellInteractionController(
		IShellPresentation presentationController,
		IShellCommandBar commandBarController,
		IShellConfirmation confirmation)
	{
		_presentationController = presentationController;
		_commandBarController = commandBarController;
		_confirmation = confirmation;
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

	// M6d: takes the real UiCommandIntent a keypress now constructs
	// (ShellInputRouter) instead of a raw display string — the first real
	// player action to reach a UiCommandIntent, not just mock content.
	public void ReportMovement(UiCommandIntent intent)
	{
		_presentationController.SetMessage(
			$"{DescribeMovement(intent.CommandId)}. " +
				"Backend movement is not connected yet.");
	}

	private static string DescribeMovement(string commandId)
	{
		return commandId switch
		{
			ExplorationMovementCommandIds.MoveForward => "Step forward",
			ExplorationMovementCommandIds.MoveBackward => "Step backward",
			ExplorationMovementCommandIds.TurnLeft => "Turn left",
			ExplorationMovementCommandIds.TurnRight => "Turn right",
			_ => commandId,
		};
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

	// M6e: each shell is deliberately distinct rather than one generic
	// "X selected" template — Encamp is a real decision (ConfirmationDialog,
	// M3's component, first live use outside dev demos), the other five
	// are one-line placeholder results. Full versions (a real Character
	// view, Spellbook, Area map) are M9's job; these are shells, not
	// screens.
	private Dictionary<string, Action> BuildExplorationCommandHandlers()
	{
		return new Dictionary<string, Action>
		{
			["move"] = EnterExplorationMovementMode,
			["view"] = () => _presentationController.SetMessage(
				"You check the party's status. " +
					"Detailed character views are not connected yet."),
			["cast"] = () => _presentationController.SetMessage(
				"You have no spells ready. " +
					"Spellcasting is not connected yet."),
			["area"] = () => _presentationController.SetMessage(
				"You get your bearings. " +
					"Area details are not connected yet."),
			["encamp"] = ShowEncampConfirmation,
			["search"] = () => _presentationController.SetMessage(
				"You search the area but find nothing. " +
					"Search mechanics are not connected yet."),
			["look"] = () => _presentationController.SetMessage(
				"You look around. " +
					"Descriptive detail is not connected yet."),
		};
	}

	private void ShowEncampConfirmation()
	{
		PushContext(ShellInteractionContext.Confirmation);

		_confirmation.ShowConfirmation(
			"Encamp",
			"Rest here and recover?",
			"Rest",
			"Cancel",
			onConfirmed: () => _presentationController.SetMessage(
				"You make camp and rest. " +
					"Rest mechanics are not connected yet."),
			onClosed: () => PopContext(ShellInteractionContext.Confirmation));
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
