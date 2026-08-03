using System;
using System.Collections.Generic;

// M9a: the shared secondary-screen shell every M9 screen (Character/
// Party, Inventory, Spellbook, Journal, Options, Save/Load) funnels
// through. The mock Area Map and mock shop/inn/services/temple screens
// were retired 2026-08-02 in favor of the real, backend-wired Area Map
// (RealSession.cs's EnterAreaMapMode) — see docs/2026-08-02-independent-
// review-and-redesign.md. ShowModalScreen pushes/pops
// ShellInteractionContext.ModalScreen the same
// way ShowEncampConfirmation already does for Confirmation
// (ShellInteractionController.Exploration.cs) — one cancel/back path
// regardless of which screen is open or how it's dismissed. Escape,
// backdrop click, and a screen's own Close button all resolve through
// ModalScreenView's Closed signal (see ModalScreenView.cs), so onClosed
// here fires exactly once no matter which of those triggered it.
internal sealed partial class ShellInteractionController
{
	// M9b: "View" already existed as an Exploration command (M6c) and only
	// ever printed a placeholder message — this is that placeholder's real
	// destination. Selecting a different party member in the list updates
	// only the body text (onRowFocused -> UpdateBody), not a full
	// re-Configure, so paging through the roster doesn't flicker the
	// screen shut and open again.
	public void ShowCharacterScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.Character(),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen },
			onRowFocused: memberId => _modalScreen.UpdateBody(
				MockSecondaryScreenContent.DescribeMember(memberId)));
	}

	// M9b: Inventory has no existing Exploration command slot (M/V/C/A/E/
	// S/L/X are all taken) — "I" was free.
	//
	// Reached from both the mock command sets (Exploration/RegionalMap)
	// and the real one (ShowRealCommands, ShellInteractionController.
	// RealSession.cs) by the same "inventory" command ID -- _activeRealSession
	// (set by ShowRealSession, cleared by the mock top-level entry points)
	// is what tells this which content to render, the same real/mock
	// branch ShellPresentationController's own _regionalMapIsRealSession
	// draws for the regional map.
	public void ShowInventoryScreen()
	{
		ShowModalScreen(
			_activeRealSession is not null
				? _activeRealSession.DescribeInventory()
				: MockSecondaryScreenContent.Inventory(),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen });
	}

	// M9c: "Cast" already existed as an Exploration command (M6c) and only
	// ever printed a placeholder message. Activating a spell in the list
	// (Enter/click, not just focus) reports a real "readied" message
	// through the shared message log behind the still-open screen — the
	// same honesty other unconnected shells use, just reachable per-item
	// here instead of only on the screen as a whole.
	public void ShowSpellbookScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.Spellbook(),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen },
			onRowFocused: spellId => _modalScreen.UpdateBody(
				MockSecondaryScreenContent.DescribeSpell(spellId)),
			onRowActivated: spellId => _presentationController.SetMessage(
				MockSecondaryScreenContent.ReadySpellMessage(spellId)));
	}

	// M9d: no existing Exploration command slot named it, but "J" (for
	// Journal itself) was free.
	public void ShowJournalScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.Journal(),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen },
			onRowFocused: entryId => _modalScreen.UpdateBody(
				MockSecondaryScreenContent.DescribeJournalEntry(entryId)));
	}

	// M9e: no existing command slot named it, but "O" (for Options itself)
	// was free. Body text reflects real, live state (ShellThemeController's
	// current theme/motion settings, PlayerInputActions' real bindings) —
	// this shell doesn't yet let the player toggle them from here, but it
	// isn't inventing fictional settings either.
	public void ShowOptionsScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.Options(
				_isHighContrastTheme(),
				_isReducedMotion()),
			new Dictionary<string, Action>
			{
				["save-load"] = ShowSaveLoadScreen,
				["close"] = CloseModalScreen,
			});
	}

	// Tracks which slot the player last focused across re-opens of this
	// screen (e.g. returning from Options), the same role
	// ShellPresentationController._selectedRegionalLocationId plays for
	// the regional map — initialized to the content's own default rather
	// than repeating that slot ID here.
	private string _selectedSaveSlotId = MockSecondaryScreenContent.DefaultSaveSlotId;

	public void ShowSaveLoadScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.SaveLoad(_selectedSaveSlotId),
			new Dictionary<string, Action>
			{
				["save"] = () => _presentationController.SetMessage(
					MockSecondaryScreenContent.SaveLoadActionMessage(
						"saved", _selectedSaveSlotId)),
				["load"] = () => _presentationController.SetMessage(
					MockSecondaryScreenContent.SaveLoadActionMessage(
						"loaded", _selectedSaveSlotId)),
				["close"] = CloseModalScreen,
			},
			onRowFocused: slotId =>
			{
				_selectedSaveSlotId = slotId;
				_modalScreen.UpdateBody(
					MockSecondaryScreenContent.DescribeSaveSlot(slotId));
			});
	}

	private void ShowModalScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused = null,
		Action<string>? onRowActivated = null)
	{
		PushContext(ShellInteractionContext.ModalScreen);
		// Must happen every open, not just the first: opening a second
		// screen from within one already open (Options -> Save/Load)
		// re-suppresses after ModalScreenView's own auto-close-then-reopen
		// briefly restores via the first screen's onClosed below — the net
		// state after the transition is still suppressed, which is what
		// matters since no frame renders in between.
		_commandBarController.SuppressShortcuts();

		_modalScreen.ShowScreen(
			model,
			commandHandlers,
			onRowFocused,
			onRowActivated,
			onClosed: () =>
			{
				PopContext(ShellInteractionContext.ModalScreen);
				_commandBarController.RestoreShortcuts();
			});
	}

	private void CloseModalScreen()
	{
		_modalScreen.CloseScreen();
	}
}
