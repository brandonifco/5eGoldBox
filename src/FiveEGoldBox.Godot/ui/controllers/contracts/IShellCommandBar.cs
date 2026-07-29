internal interface IShellCommandBar
{
	void ShowCommands(params CommandDefinition[] commands);

	void ShowMovementPrompt();

	void Refresh();

	// M9e: a ModalScreen's own action buttons (Close, Save, etc.) use
	// HotkeyCommandButton/Shortcut too, same as the command bar underneath
	// them — and Godot's Shortcut matching fires on any visible, enabled
	// button regardless of z-order or focus, not just the topmost one.
	// Without this, a modal reusing a letter the command bar behind it
	// already bound (e.g. both "Close" and "Cast" on "C") triggers both,
	// which is exactly the bug found screenshot-verifying M9e. The
	// backdrop already blocks mouse input to what's behind it this way;
	// this is the keyboard equivalent.
	void SuppressShortcuts();

	void RestoreShortcuts();
}
