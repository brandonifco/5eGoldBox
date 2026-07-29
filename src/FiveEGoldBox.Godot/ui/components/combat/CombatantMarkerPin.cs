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

	private Color _ringColor = AllyPalette[0];
	private bool _active;
	private bool _selected;

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
		bool selected)
	{
		TooltipText = label;
		_ringColor = isAlly
			? AllyPalette[allyIndex % AllyPalette.Length]
			: EnemyColor;
		_active = active;
		_selected = selected;

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
	}

	public static float MarkerDiameter => Diameter;
}
