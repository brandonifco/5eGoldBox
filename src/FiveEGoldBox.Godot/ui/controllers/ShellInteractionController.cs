using System;
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
		EnterExploration(_presentationController.ShowExploration);
	}

	// M7e: shared by the boot/dev-shortcut entry point above and the
	// regional map's "Enter" command (ShellInteractionController.
	// RegionalMap.cs) — same context reset and command-bar setup either
	// way, only which presentation call fires differs.
	private void EnterExploration(Action showPresentation)
	{
		ResetContext();
		showPresentation();
		ShowExplorationCommands();
	}

	public void ShowRegionalMap()
	{
		// M7b: SelectionList, not CommandMenu — the location list/cursor
		// is this mode's ambient default interaction, the same way
		// CommandMenu is Exploration's, so zoom keys (gated on this
		// context in ShellInputRouter) are live as soon as the map shows.
		ResetContext(ShellInteractionContext.SelectionList);
		_presentationController.ShowRegionalMap();
		ShowRegionalMapCommands();
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

	private void ResetContext(
		ShellInteractionContext context = ShellInteractionContext.CommandMenu)
	{
		_contextStack.Clear();
		_contextStack.Push(context);
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
