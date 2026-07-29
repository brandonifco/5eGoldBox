using System;
using System.Collections.Generic;

internal interface IShellModalScreen
{
	void ShowScreen(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers,
		Action<string>? onRowFocused = null,
		Action<string>? onRowActivated = null,
		Action? onClosed = null);

	void CloseScreen();
}
