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

	// The medieval-heroes pack's _MVsv_alt_*.png strips are three
	// 128x128 frames side by side; only the first is used for a static
	// portrait (no animation wiring yet).
	private const float PortraitFrameSize = 128f;

	private Color _ringColor = AllyPalette[0];
	private bool _active;
	private bool _selected;
	private int? _currentHitPoints;
	private int? _maximumHitPoints;
	private Texture2D? _portrait;
	private bool _flipPortrait;

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
		int? maximumHitPoints = null,
		Texture2D? portrait = null,
		bool flipPortrait = false)
	{
		TooltipText = label;
		_ringColor = isAlly
			? AllyPalette[allyIndex % AllyPalette.Length]
			: EnemyColor;
		_active = active;
		_selected = selected;
		_currentHitPoints = currentHitPoints;
		_maximumHitPoints = maximumHitPoints;
		_portrait = portrait;
		_flipPortrait = flipPortrait;

		if (IsNodeReady())
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2f;
		float radius = Mathf.Min(Size.X, Size.Y) / 2f;

		if (_portrait is Texture2D portrait)
		{
			DrawPortrait(center, radius, portrait);
		}

		if (_active || _selected)
		{
			DrawCircle(center, radius * 0.7f, new Color(_ringColor, 0.25f));
		}

		float borderWidth = _selected ? 4f : _active ? 3f : 2f;
		DrawArc(center, radius, 0, Mathf.Tau, 32, _ringColor, borderWidth, antialiased: true);

		DrawHealthArc(center, radius);
	}

	// Draws only the first of the portrait's three 128x128 idle frames --
	// a static token image, not an animation. Slightly larger than the
	// ring diameter (a bust/full-body sprite reads as "standing behind"
	// the ring, not clipped by it) and drawn before every other layer so
	// the ring/health arc/highlight still render on top, unchanged.
	//
	// No per-direction art exists (single static frame, see above), so
	// facing the other way means literally mirroring the draw call.
	// _flipPortrait is computed by the caller (CombatView.Markers.cs's
	// ShouldFlipPortrait, which also owns the "which side's art faces
	// which way by default" convention -- this class only knows how to
	// mirror, not why). A temporary scale.x=-1 transform, centered on
	// the portrait so the mirror axis is its own middle rather than the
	// control's corner, reset to identity immediately after so it can't
	// leak into the ring/arc draws that follow.
	private void DrawPortrait(Vector2 center, float radius, Texture2D portrait)
	{
		float portraitSize = radius * 2.2f;
		Rect2 destinationRect = new(
			center - new Vector2(portraitSize / 2f, portraitSize / 2f),
			new Vector2(portraitSize, portraitSize));
		Rect2 sourceRect = new(Vector2.Zero, new Vector2(PortraitFrameSize, PortraitFrameSize));

		if (!_flipPortrait)
		{
			DrawTextureRectRegion(portrait, destinationRect, sourceRect);
			return;
		}

		DrawSetTransform(center, 0f, new Vector2(-1f, 1f));
		DrawTextureRectRegion(
			portrait,
			new Rect2(destinationRect.Position - center, destinationRect.Size),
			sourceRect);
		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
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
