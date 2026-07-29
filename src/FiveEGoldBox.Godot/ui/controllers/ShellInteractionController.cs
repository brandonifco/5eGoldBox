using System.Collections.Generic;
using Godot;

// Split into partials by concern once this crossed the governing plan's
// 250-line review threshold (§6.3), flagged as "worth watching" back at
// M6e (244 lines) and crossed by M6f's facing/overlay wiring — same
// per-concern partial-class convention AppShell itself already uses
// (AppShell.Presentation.cs, AppShell.Commands.cs, etc.). This file keeps
// shell navigation and the interaction-context stack; the exploration
// command shells (View/Cast/Area/Encamp/Search/Look, movement mode) moved
// to ShellInteractionController.Exploration.cs. Pure code motion — no
// behavior changed by the split itself.
internal sealed partial class ShellInteractionController : IShellInteractionState
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
