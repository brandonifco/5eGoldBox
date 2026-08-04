using Godot;

// A translucent square marking one grid cell's tactical significance —
// script-only, mirrors CombatantMarkerPin's role but for empty cells
// rather than combatants. "valid-target"/"move-range" (attack/spell
// targeting, and mock combat's own move targeting) are real destinations
// (focusable/clickable); "invalid-target" is shown but never focusable,
// the same Disabled-entry precedent SelectionList already established
// (M4/M5) for "visible, not choosable."
//
// "move-legal"/"move-illegal" are a different kind of cell entirely —
// used for real-combat move targeting's full-battlefield keyboard
// cursor rather than a pre-highlighted set of legal destinations (the
// user asked for this directly: no more lighting up every reachable
// tile, just a single cursor that moves with the keyboard and reads
// legal/illegal per tile it's actually on). These draw nothing at all
// unless focused, then a diamond outline in the tile's own shape
// instead of a filled diamond -- legal cells are still real Buttons the
// same way "valid-target" cells are, illegal ones too (so the cursor
// can actually traverse them and report why), just never filled.
public partial class CombatHighlightCell : Button
{
	// Internal, not private -- CombatView.Markers.cs's own mouse-hover
	// outline (DrawGridLines) reuses these so a hovered "move-legal"/
	// "move-illegal" cell reads the same yellow/red as the keyboard
	// cursor, rather than its own separately-chosen color drifting from
	// this one over time.
	internal static readonly Color CursorLegalColor = new(0.95f, 0.85f, 0.25f, 1f);
	internal static readonly Color CursorIllegalColor = new(0.85f, 0.25f, 0.25f, 1f);

	private Color _fillColor;
	private bool _isCursorOnly;
	private Color _cursorColor;

	public override void _Ready()
	{
		ThemeTypeVariation = "CombatMarkerButton";
		MouseDefaultCursorShape = CursorShape.PointingHand;
		// Suppresses Godot's own rectangular focus stylebox (the theme's
		// shared CombatMarkerButton/styles/focus, a plain rectangle) --
		// this class draws its own diamond-shaped cursor outline in
		// _Draw instead, matching the tile's real shape. Reusing the
		// already-empty "normal" stylebox rather than adding a new theme
		// resource. Applies to every kind, not just the cursor-only
		// ones -- the same stray rectangle would show through a filled
		// "valid-target" diamond too, just unreported until now.
		AddThemeStyleboxOverride("focus", GetThemeStylebox("normal"));
		FocusEntered += QueueRedraw;
		FocusExited += QueueRedraw;
	}

	public void Configure(string kind)
	{
		(_fillColor, bool selectable, _isCursorOnly, _cursorColor) = ResolveStyle(kind);
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
		Vector2[] outline = { points[0], points[1], points[2], points[3], points[0] };

		if (_isCursorOnly)
		{
			if (HasFocus())
			{
				DrawPolyline(outline, _cursorColor, width: 2.5f, antialiased: true);
			}

			return;
		}

		DrawColoredPolygon(points, _fillColor);
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

	private static (Color Fill, bool Selectable, bool IsCursorOnly, Color CursorColor) ResolveStyle(
		string kind)
	{
		return kind switch
		{
			"move-legal" => (Colors.Transparent, true, true, CursorLegalColor),
			"move-illegal" => (Colors.Transparent, true, true, CursorIllegalColor),
			// Mock combat's own move targeting (MockCombatContent.
			// MoveRangeHighlights) still produces this -- real combat's
			// move targeting is the only thing that moved to the cursor
			// model above.
			"move-range" => (new Color(0.4f, 0.6f, 0.9f, 0.35f), true, false, default),
			"valid-target" => (new Color(0.85f, 0.25f, 0.25f, 0.4f), true, false, default),
			"invalid-target" => (new Color(0.4f, 0.4f, 0.4f, 0.35f), false, false, default),
			_ => (new Color(0.6f, 0.6f, 0.6f, 0.3f), false, false, default),
		};
	}
}
