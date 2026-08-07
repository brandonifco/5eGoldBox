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
// unless focused, then an outline in the tile's own shape instead of a
// filled cell -- legal cells are still real Buttons the same way
// "valid-target" cells are, illegal ones too (so the cursor can actually
// traverse them and report why), just never filled.
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
		// Suppresses the theme's shared CombatMarkerButton/styles/focus
		// stylebox so it never double-draws over this class's own cursor
		// outline in _Draw. Reusing the already-empty "normal" stylebox
		// rather than adding a new theme resource. Still needed on the
		// square grid: the theme's focus style is a 2px amber rect that
		// would otherwise sit on top of, and disagree with, the
		// legal/illegal cursor colour.
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

	// The cell fills its whole node rect now that the grid is square. The
	// isometric version drew a diamond inset in that rect and needed a
	// matching _HasPoint override, since Control's default hit-test is
	// the rectangular bounding box and would otherwise have registered
	// clicks on a diamond's invisible corners. Both are gone: the drawn
	// shape and the default hit-test are the same rectangle again.
	public override void _Draw()
	{
		Rect2 rect = new(Vector2.Zero, Size);

		if (_isCursorOnly)
		{
			if (HasFocus())
			{
				DrawRect(rect, _cursorColor, filled: false, width: 2.5f);
			}

			return;
		}

		DrawRect(rect, _fillColor);
		DrawRect(rect, new Color(_fillColor, 0.9f), filled: false, width: 1.5f);
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
