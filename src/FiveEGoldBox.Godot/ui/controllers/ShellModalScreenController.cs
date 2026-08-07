using System;
using System.Collections.Generic;

// Wraps ModalScreenView (M9a) the same way ShellConfirmationController
// wraps ConfirmationDialog — constructed independently of
// ShellInteractionController, which owns the interaction-context stack
// and pushes/pops ModalScreen around whatever this shows.
internal sealed class ShellModalScreenController : IShellModalScreen
{
	private readonly ModalScreenView _view;

	public ShellModalScreenController(ModalScreenView view)
	{
		_view = view;
	}

	public void ShowScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused = null,
		Action<string>? onRowActivated = null,
		Action? onClosed = null,
		Action<string>? onTextChanged = null,
		Action? onTextSubmitted = null)
	{
		_view.ShowScreen(
			model,
			commandHandlers,
			onRowFocused,
			onRowActivated,
			onClosed,
			onTextChanged,
			onTextSubmitted);
	}

	public void CloseScreen()
	{
		_view.Close();
	}

	public void UpdateBody(string? bodyText)
	{
		_view.UpdateBody(bodyText);
	}
}
