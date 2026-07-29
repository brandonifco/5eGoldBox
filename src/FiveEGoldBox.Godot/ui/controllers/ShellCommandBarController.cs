using System;
using System.Collections.Generic;
using Godot;

internal sealed class ShellCommandBarController : IShellCommandBar
{
	private readonly CommandBar _standardCommandBar;
	private readonly CommandBar _immersiveCommandBar;
	private readonly IShellLayoutState _layoutState;
	private readonly PackedScene _commandButtonScene;

	private bool _showingMovementPrompt;
	private bool _shortcutsSuppressed;
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
		ValidateUniqueShortcuts(commands);

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
			enableShortcuts: !isImmersive && !_shortcutsSuppressed);
		RenderCommands(
			_immersiveCommandBar,
			_currentCommands,
			enableShortcuts: isImmersive && !_shortcutsSuppressed);
	}

	// A modal screen's own action buttons use the same HotkeyCommandButton/
	// Shortcut mechanism as the command bar underneath them, and Godot
	// matches a Shortcut on any visible, enabled button — regardless of
	// z-order or focus, not just the topmost one. Without suppressing the
	// bar's own shortcuts while a modal is open, a modal reusing a letter
	// the bar already bound (e.g. both "Close" and "Cast" on "C") would
	// trigger both. The backdrop already blocks mouse input to what's
	// behind it this way; this is the keyboard equivalent.
	public void SuppressShortcuts()
	{
		_shortcutsSuppressed = true;
		Refresh();
	}

	public void RestoreShortcuts()
	{
		_shortcutsSuppressed = false;
		Refresh();
	}

	private void RenderCommands(
		CommandBar commandBar,
		CommandDefinition[] commands,
		bool enableShortcuts)
	{
		commandBar.Clear();

		HotkeyCommandButton? firstButton = null;

		foreach (CommandDefinition command in commands)
		{
			HotkeyCommandButton button =
				CreateCommandButton(command, enableShortcuts);
			commandBar.AddContent(button);
			firstButton ??= button;
		}

		// enableShortcuts is true only for the currently visible layout's
		// bar, so it also gates which bar gets deterministic initial
		// focus — the hidden layout's bar must not steal it.
		if (enableShortcuts && firstButton is not null)
		{
			// Guarded rather than a bare CallDeferred(GrabFocus): if
			// ShowCommands is called again before this deferred call fires
			// (e.g. RealGameSession's initial command set immediately
			// replaced by a mode switch in the same frame — M9's screenshot
			// harness does exactly this), Clear() above will already have
			// QueueFree'd this exact button by the time it runs, and
			// grab_focus on a freed control logs a real engine error
			// ("!is_inside_tree()") — the same race ModalBackdrop.
			// RestorePreviousFocus already guards against.
			HotkeyCommandButton buttonToFocus = firstButton;

			Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(buttonToFocus) &&
					buttonToFocus.IsInsideTree())
				{
					buttonToFocus.GrabFocus();
				}
			}).CallDeferred();
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

	private static void ValidateUniqueShortcuts(CommandDefinition[] commands)
	{
		HashSet<Key> seenShortcuts = new();

		foreach (CommandDefinition command in commands)
		{
			if (!seenShortcuts.Add(command.ShortcutKey))
			{
				throw new ArgumentException(
					$"Command '{command.Name}' reuses shortcut " +
						$"'{command.ShortcutKey}' within the same active " +
						"command set.",
					nameof(commands));
			}
		}
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
