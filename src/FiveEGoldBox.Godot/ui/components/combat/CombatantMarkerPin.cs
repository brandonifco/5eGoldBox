using Godot;

// A real, focusable/clickable marker for one combatant on the tactical
// grid — script-only (no .tscn), same reasoning as RegionalMapMarkerPin.
// Unlike that one, this IS interactive (a Button): targeting an enemy
// combatant is direct-click/Enter on the combatant itself, not routed
// through a side list the way the regional map's locations were — there
// is no natural "list" of grid positions to browse in tactical combat.
// CombatMarkerButton (theme) makes the Button itself fully invisible;
// every visual is this class's own _Draw, and Godot's default focus
// rectangle stays off so it never fights the hand-drawn ring.
public partial class CombatantMarkerPin : Button
{
	private const float Diameter = 34f;

	// Matches the mockup's own ring colors (green/blue/yellow/cyan/
	// purple/orange) cycling by ally ordinal, not by character identity —
	// there is no "this color belongs to Aric specifically" rule, just a
	// fixed palette assigned in list order.
	private static readonly Color[] AllyPalette =
	{
		new(0.36f, 0.62f, 0.86f),
		new(0.86f, 0.72f, 0.30f),
		new(0.40f, 0.75f, 0.42f),
		new(0.86f, 0.55f, 0.28f),
		new(0.38f, 0.78f, 0.85f),
		new(0.68f, 0.42f, 0.78f),
	};

	private static readonly Color EnemyColor = new(0.85f, 0.25f, 0.25f);

	// Real combat's arc lands at 12 o'clock and sweeps clockwise, the way
	// a health ring conventionally depletes — not the ring border's own
	// 0-to-Tau sweep, which is purely decorative and has no "start" a
	// viewer reads meaning into.
	private const float HealthArcStartAngle = -Mathf.Pi / 2f;

	private Color _ringColor = AllyPalette[0];
	private bool _active;
	private bool _selected;
	private int? _currentHitPoints;
	private int? _maximumHitPoints;

	public override void _Ready()
	{
		FocusMode = FocusModeEnum.All;
		ThemeTypeVariation = "CombatMarkerButton";
		MouseDefaultCursorShape = CursorShape.PointingHand;
	}

	public void Configure(
		string label,
		int allyIndex,
		bool isAlly,
		bool active,
		bool selected,
		int? currentHitPoints = null,
		int? maximumHitPoints = null)
	{
		TooltipText = label;
		_ringColor = isAlly
			? AllyPalette[allyIndex % AllyPalette.Length]
			: EnemyColor;
		_active = active;
		_selected = selected;
		_currentHitPoints = currentHitPoints;
		_maximumHitPoints = maximumHitPoints;

		if (IsNodeReady())
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2f;
		float radius = Mathf.Min(Size.X, Size.Y) / 2f;

		if (_active || _selected)
		{
			DrawCircle(center, radius * 0.7f, new Color(_ringColor, 0.25f));
		}

		float borderWidth = _selected ? 4f : _active ? 3f : 2f;
		DrawArc(center, radius, 0, Mathf.Tau, 32, _ringColor, borderWidth, antialiased: true);

		DrawHealthArc(center, radius);
	}

	// Real combat only — mock content never had HP to show. A thin arc
	// just inside the ring, proportional to the remaining fraction, red-
	// shifting as it drops — reuses the same DrawArc primitive as the
	// ring itself rather than a new bar/border widget, so this stays
	// within "no new art assets."
	private void DrawHealthArc(Vector2 center, float radius)
	{
		if (_currentHitPoints is not int current ||
			_maximumHitPoints is not int maximum ||
			maximum <= 0)
		{
			return;
		}

		float fraction = Mathf.Clamp((float)current / maximum, 0f, 1f);

		if (fraction <= 0f)
		{
			return;
		}

		Color healthColor = fraction switch
		{
			>= 0.5f => new Color(0.42f, 0.82f, 0.42f),
			>= 0.25f => new Color(0.86f, 0.72f, 0.30f),
			_ => new Color(0.86f, 0.25f, 0.25f),
		};

		DrawArc(
			center,
			radius - 4f,
			HealthArcStartAngle,
			HealthArcStartAngle + (Mathf.Tau * fraction),
			24,
			healthColor,
			3f,
			antialiased: true);
	}

	public static float MarkerDiameter => Diameter;
}
