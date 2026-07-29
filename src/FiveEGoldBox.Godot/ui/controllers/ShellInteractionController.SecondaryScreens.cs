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
