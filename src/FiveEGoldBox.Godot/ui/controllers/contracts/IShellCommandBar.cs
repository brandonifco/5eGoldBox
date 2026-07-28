internal interface IShellCommandBar
{
	void ShowCommands(params CommandDefinition[] commands);

	void ShowMovementPrompt();

	void Refresh();
}
