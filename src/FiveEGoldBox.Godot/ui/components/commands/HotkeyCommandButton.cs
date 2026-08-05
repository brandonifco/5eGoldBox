using System;
using Godot;

public partial class HotkeyCommandButton : Button
{
	private RichTextLabel _commandLabel = null!;
	private Action _handler = null!;
	private bool _hasHandler;
	private string _formattedLabel = string.Empty;

	public override void _Ready()
	{
		_commandLabel = GetNode<RichTextLabel>("%CommandLabel");
		ApplyContent();
	}

	public void Configure(
		string commandName,
		string formattedLabel,
		Key shortcutKey,
		Action handler,
		bool enableShortcut)
	{
		Name = $"{commandName.Replace(" ", string.Empty)}Button";
		TooltipText = $"{commandName} ({shortcutKey})";
		_formattedLabel = formattedLabel;

		if (_hasHandler)
		{
			Pressed -= _handler;
		}

		_handler = handler;
		_hasHandler = true;
		Pressed += _handler;

		Shortcut = enableShortcut
			? CreateShortcut(shortcutKey)
			: null;

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	// Bold alone (the label's own [b]X[/b] markup around its hotkey
	// letter) wasn't visually distinct enough against ShellCommandText's
	// uniform gold -- every FormattedLabel already brackets its hotkey in
	// [b]/[/b], so recoloring just that span here covers every caller at
	// once rather than touching each hand-authored string individually.
	// The rest of the label keeps the theme's own default gold, untouched.
	private void ApplyContent()
	{
		_commandLabel.Text = _formattedLabel
			.Replace("[b]", "[b][color=#FFFFFF]")
			.Replace("[/b]", "[/color][/b]");
	}

	private static Shortcut CreateShortcut(Key shortcutKey)
	{
		Godot.Collections.Array events = new()
		{
			new InputEventKey
			{
				Keycode = shortcutKey,
			},
		};

		return new Shortcut
		{
			Events = events,
		};
	}
}
