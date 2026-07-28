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

	private void ApplyContent()
	{
		_commandLabel.Text = _formattedLabel;
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
