using Godot;

// A translucent square marking one grid cell's tactical significance —
// script-only, mirrors CombatantMarkerPin's role but for empty cells
// rather than combatants. "move-range" and "valid-target" are real
// destinations (focusable/clickable); "invalid-target" is shown but
// never focusable, the same Disabled-entry precedent SelectionList
// already established (M4/M5) for "visible, not choosable."
public partial class CombatHighlightCell : Button
{
	private Color _fillColor;

	public override void _Ready()
	{
		ThemeTypeVariation = "CombatMarkerButton";
		MouseDefaultCursorShape = CursorShape.PointingHand;
	}

	public void Configure(string kind)
	{
		(_fillColor, bool selectable) = ResolveStyle(kind);
		FocusMode = selectable ? FocusModeEnum.All : FocusModeEnum.None;
		Disabled = !selectable;

		if (IsNodeReady())
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), _fillColor);
		DrawRect(
			new Rect2(Vector2.Zero, Size),
			new Color(_fillColor, 0.9f),
			filled: false,
			width: 1.5f);
	}

	private static (Color Fill, bool Selectable) ResolveStyle(string kind)
	{
		return kind switch
		{
			"move-range" => (new Color(0.4f, 0.6f, 0.9f, 0.35f), true),
			"valid-target" => (new Color(0.85f, 0.25f, 0.25f, 0.4f), true),
			"invalid-target" => (new Color(0.4f, 0.4f, 0.4f, 0.35f), false),
			_ => (new Color(0.6f, 0.6f, 0.6f, 0.3f), false),
		};
	}
}
