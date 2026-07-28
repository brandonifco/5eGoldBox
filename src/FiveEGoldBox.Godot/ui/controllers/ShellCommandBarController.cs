using System;
using Godot;

internal sealed class ShellCommandBarController : IShellCommandBar
{
	private readonly CommandBar _standardCommandBar;
	private readonly CommandBar _immersiveCommandBar;
	private readonly IShellLayoutState _layoutState;
	private readonly PackedScene _commandButtonScene;

	private bool _showingMovementPrompt;
	private CommandDefinition[] _currentCommands =
		Array.Empty<CommandDefinition>();

	public ShellCommandBarController(
		CommandBar standardCommandBar,
		CommandBar immersiveCommandBar,
		IShellLayoutState layoutState,
		PackedScene commandButtonScene)
	{
		_standardCommandBar = standardCommandBar;
		_immersiveCommandBar = immersiveCommandBar;
		_layoutState = layoutState;
		_commandButtonScene = commandButtonScene;
	}

	public void ShowCommands(params CommandDefinition[] commands)
	{
		_showingMovementPrompt = false;
		_currentCommands = commands;
		Refresh();
	}

	public void ShowMovementPrompt()
	{
		_showingMovementPrompt = true;
		_currentCommands = Array.Empty<CommandDefinition>();
		Refresh();
	}

	public void Refresh()
	{
		if (_showingMovementPrompt)
		{
			RenderMovementPrompt(_standardCommandBar);
			RenderMovementPrompt(_immersiveCommandBar);
			return;
		}

		bool isImmersive = _layoutState.IsImmersive;

		RenderCommands(
			_standardCommandBar,
			_currentCommands,
			enableShortcuts: !isImmersive);
		RenderCommands(
			_immersiveCommandBar,
			_currentCommands,
			enableShortcuts: isImmersive);
	}

	private void RenderCommands(
		CommandBar commandBar,
		CommandDefinition[] commands,
		bool enableShortcuts)
	{
		commandBar.Clear();

		foreach (CommandDefinition command in commands)
		{
			commandBar.AddContent(
				CreateCommandButton(command, enableShortcuts));
		}
	}

	private HotkeyCommandButton CreateCommandButton(
		CommandDefinition command,
		bool enableShortcut)
	{
		HotkeyCommandButton button =
			_commandButtonScene.Instantiate<HotkeyCommandButton>();

		button.Configure(
			command.Name,
			command.FormattedLabel,
			command.ShortcutKey,
			command.Handler,
			enableShortcut);

		return button;
	}

	private static void RenderMovementPrompt(
		CommandBar commandBar)
	{
		commandBar.Clear();

		RichTextLabel prompt = new()
		{
			ThemeTypeVariation = "ShellCommandText",
			BbcodeEnabled = true,
			CustomMinimumSize = new Vector2(0, 40),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FitContent = false,
			ScrollActive = false,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,
			Text =
				"[b]ARROWS/NUMPAD[/b] TO MOVE — " +
				"[b]ESC/SPACE[/b] TO EXIT",
		};

		commandBar.AddContent(prompt);
	}
}
