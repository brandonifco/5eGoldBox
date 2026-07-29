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
		Vector2[] points = DiamondPoints(Size);
		DrawColoredPolygon(points, _fillColor);

		Vector2[] outline = { points[0], points[1], points[2], points[3], points[0] };
		DrawPolyline(outline, new Color(_fillColor, 0.9f), width: 1.5f, antialiased: true);
	}

	private static Vector2[] DiamondPoints(Vector2 size)
	{
		Vector2 half = size / 2f;
		return new[]
		{
			new Vector2(half.X, 0f),     // top
			new Vector2(size.X, half.Y), // right
			new Vector2(half.X, size.Y), // bottom
			new Vector2(0f, half.Y),     // left
		};
	}

	// A rhombus centered at (half.X, half.Y) with half-extents `half` is
	// the L1 ("taxicab") unit ball scaled by `half`:
	// |dx|/half.X + |dy|/half.Y <= 1. Exact for a rhombus, O(1) — no
	// generic point-in-polygon test needed. Without this override,
	// Control's default hit-test is the full rectangular bounding box,
	// so clicking a diamond's invisible corner would wrongly register as
	// a click on this cell.
	public override bool _HasPoint(Vector2 point)
	{
		Vector2 half = Size / 2f;

		if (half.X <= 0f || half.Y <= 0f)
		{
			return false;
		}

		Vector2 centered = point - half;
		return (Mathf.Abs(centered.X) / half.X) + (Mathf.Abs(centered.Y) / half.Y) <= 1f;
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
