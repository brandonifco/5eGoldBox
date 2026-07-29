using System;
using System.Collections.Generic;

// M9a: the shared secondary-screen shell every M9 screen (Character/
// Party, Inventory, Spellbook, Area Map, Journal, Options, Save/Load, and
// the reusable location-interaction screen) funnels through.
// ShowModalScreen pushes/pops ShellInteractionContext.ModalScreen the same
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
	public void ShowInventoryScreen()
	{
		ShowModalScreen(
			MockSecondaryScreenContent.Inventory(),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen });
	}

	private void ShowModalScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused = null,
		Action<string>? onRowActivated = null)
	{
		PushContext(ShellInteractionContext.ModalScreen);

		_modalScreen.ShowScreen(
			model,
			commandHandlers,
			onRowFocused,
			onRowActivated,
			onClosed: () => PopContext(ShellInteractionContext.ModalScreen));
	}

	private void CloseModalScreen()
	{
		_modalScreen.CloseScreen();
	}
}
