using System;
using System.Collections.Generic;
using System.Linq;

// Split into partials by concern once this crossed the governing plan's
// 250-line review threshold (§6.3), flagged as "worth watching" back at
// M6e (244 lines) and crossed by M6f's facing/overlay wiring — same
// per-concern partial-class convention AppShell itself already uses
// (AppShell.Presentation.cs, AppShell.Commands.cs, etc.). This file keeps
// shell navigation and the interaction-context stack; the exploration
// command shells (View/Cast/Area/Encamp/Search/Look, movement mode) moved
// to ShellInteractionController.Exploration.cs, the real-session
// integration (RealGameSession, PR #170-172) moved to
// ShellInteractionController.RealSession.cs, and M9's secondary-screen
// entry points (Character/Party, Inventory, Spellbook, Journal, Options,
// Save/Load, location interactions) are in
// ShellInteractionController.SecondaryScreens.cs. Pure code motion — no
// behavior changed by the split itself.
internal sealed partial class ShellInteractionController : IShellInteractionState
{
	private readonly IShellPresentation _presentationController;
	private readonly IShellCommandBar _commandBarController;
	private readonly IShellConfirmation _confirmation;
	private readonly IShellModalScreen _modalScreen;
	private readonly IShellPartyPreview _partyPreview;
	private readonly Func<bool> _isHighContrastTheme;
	private readonly Func<bool> _isReducedMotion;
	private readonly Stack<ShellInteractionContext> _contextStack = new();

	public ShellInteractionController(
		IShellPresentation presentationController,
		IShellCommandBar commandBarController,
		IShellConfirmation confirmation,
		IShellModalScreen modalScreen,
		IShellPartyPreview partyPreview,
		Func<bool> isHighContrastTheme,
		Func<bool> isReducedMotion)
	{
		_presentationController = presentationController;
		_commandBarController = commandBarController;
		_confirmation = confirmation;
		_modalScreen = modalScreen;
		_partyPreview = partyPreview;
		_isHighContrastTheme = isHighContrastTheme;
		_isReducedMotion = isReducedMotion;
		_contextStack.Push(ShellInteractionContext.CommandMenu);

		_presentationController.CombatantTargeted += OnCombatantTargeted;
		_presentationController.CombatCellTargeted += OnCombatCellTargeted;
		_presentationController.CombatCellCursorFocused += OnCombatCellCursorFocused;
	}

	public ShellInteractionContext CurrentContext => _contextStack.Peek();

	public void ShowExploration()
	{
		EnterExploration(_presentationController.ShowExploration);
	}

	// M7e: shared by the boot/dev-shortcut entry point above and the
	// regional map's "Enter" command (ShellInteractionController.
	// RegionalMap.cs) — same context reset and command-bar setup either
	// way, only which presentation call fires differs. Mock-only, like
	// ShowRegionalMap/ShowCombat below — real exploration entry goes
	// through ShowRealSession instead — so this is where _activeRealSession
	// resets, the same role _regionalMapIsRealSession's own reset plays
	// in the mock ShowRegionalMap() on ShellPresentationController.
	private void EnterExploration(Action showPresentation)
	{
		ResetContext();
		_activeRealSession = null;
		_partyPreview.ShowMockPreview();
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
		_activeRealSession = null;
		_partyPreview.ShowMockPreview();
		_presentationController.ShowRegionalMap();
		ShowRegionalMapCommands();
	}

	public void ShowCombat()
	{
		ResetContext();
		_activeRealSession = null;
		_partyPreview.ShowMockPreview();
		_presentationController.ShowCombat();
		ShowCombatCommands();
	}

	public void ExitExplorationMovementMode()
	{
		PopContext(ShellInteractionContext.DirectMovement);

		if (_activeRealMovementSession is not null)
		{
			RealGameSession session = _activeRealMovementSession;

			_activeRealMovementSession = null;
			// Only ever set alongside _activeRealMovementSession when
			// movement was entered via the area map (EnterAreaMapMode) —
			// plain front-facing movement (EnterRealMovementMode) never
			// sets it, so this is a no-op there.
			_activeAreaMapSession = null;
			ShowRealSession(session, "Movement mode ended.");
			return;
		}

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
		// A top-level screen switch (ShowExploration/ShowRegionalMap/
		// ShowCombat/ShowRealSession, every one of which calls this first)
		// can happen while a ModalScreen is still open — the F1/F2/F3 dev
		// view shortcuts are live at all times, not gated by the command-
		// bar shortcut suppression M9e's ShowModalScreen relies on for
		// everything else. Found screenshot-verifying M9f: switching
		// screens this way left the stale modal showing over whatever
		// loaded underneath it, since ResetContext previously just
		// discarded the stack without telling ModalScreenView to close.
		if (_contextStack.Contains(ShellInteractionContext.ModalScreen))
		{
			_modalScreen.CloseScreen();
		}

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
