using System;
using Godot;

public partial class HotkeyCommandButton : Button
{
	// Left/right breathing room around the measured label, so buttons
	// don't read as cramped against the ShellCommandButton border.
	private const float HorizontalPaddingPx = 26f;
	private const float MinimumHeightPx = 34f;
	private const float FallbackCharWidthPx = 9f;

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

		ApplyMeasuredWidth();
	}

	// CommandLabel is anchored to this Button's full rect rather than
	// being a layout child, so it reports no minimum size upward. That was
	// invisible while the button carried size_flags_horizontal = EXPAND|
	// FILL and took an equal share of the bar regardless. Once buttons
	// became content-sized, the button's only minimum was its own
	// custom_minimum_size -- zero width -- and every button collapsed to a
	// sliver.
	//
	// Measuring the plain text against the label's own resolved theme font
	// is deterministic and needs no layout pass, unlike asking the label
	// for its content width after the fact.
	private void ApplyMeasuredWidth()
	{
		string plain = StripMarkup(_formattedLabel);
		float textWidth = MeasureText(plain);

		CustomMinimumSize = new Vector2(
			Mathf.Ceil(textWidth) + HorizontalPaddingPx,
			MinimumHeightPx);
	}

	private float MeasureText(string plain)
	{
		Font? font = _commandLabel.GetThemeFont("normal_font", "RichTextLabel");
		int fontSize = _commandLabel.GetThemeFontSize("normal_font_size", "RichTextLabel");

		if (font is null || fontSize <= 0)
		{
			// Only reachable if the theme stops supplying a font for
			// RichTextLabel; a rough per-character estimate keeps the bar
			// usable rather than collapsing it again.
			return plain.Length * FallbackCharWidthPx;
		}

		return font.GetStringSize(
			plain,
			HorizontalAlignment.Left,
			width: -1,
			fontSize: fontSize).X;
	}

	// The label's text is BBCode; only the visible characters should count
	// toward the measured width.
	private static string StripMarkup(string formatted)
	{
		System.Text.StringBuilder builder = new(formatted.Length);
		bool insideTag = false;

		foreach (char character in formatted)
		{
			if (character == '[')
			{
				insideTag = true;
			}
			else if (character == ']')
			{
				insideTag = false;
			}
			else if (!insideTag)
			{
				builder.Append(character);
			}
		}

		return builder.ToString();
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
