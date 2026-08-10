using System;
using System.Collections.Generic;

internal interface IShellModalScreen
{
	void ShowScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused = null,
		Action<string>? onRowActivated = null,
		Action? onClosed = null,
		Action<string>? onTextChanged = null,
		Action? onTextSubmitted = null);

	void CloseScreen();

	void UpdateBody(string? bodyText);

	void UpdateCommands(
		IReadOnlyList<CommandViewModel> commands,
		IReadOnlyDictionary<string, Action> commandHandlers);
}
