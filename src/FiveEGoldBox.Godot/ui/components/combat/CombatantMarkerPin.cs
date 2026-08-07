using Godot;

// A real, focusable/clickable marker for one combatant on the tactical
// grid — script-only (no .tscn), same reasoning as RegionalMapMarkerPin.
// Unlike that one, this IS interactive (a Button): targeting an enemy
// combatant is direct-click/Enter on the combatant itself, not routed
// through a side list the way the regional map's locations were — there
// is no natural "list" of grid positions to browse in tactical combat.
// CombatMarkerButton (theme) makes the Button itself fully invisible;
// every visual is this class's own _Draw.
//
// This used to draw a coloured ring around each combatant, with a health
// arc sweeping around its inside and a per-ally colour cycling through a
// six-entry palette. All of it is gone. The ring existed because an
// isometric diamond gives a sprite no clear footprint, so something had
// to mark where a combatant actually stood while also carrying HP and
// side; on a square grid the tile itself does that job. The user's own
// word for the rings was "unnecessary", and the Gold Box originals had
// nothing of the kind — combatants simply occupied a square.
//
// What replaces it is flatter and reads at a glance: the portrait sits
// on its tile, a hairline strip along the bottom edge gives the side,
// and a small bar above that gives health. Colours come from the shell's
// EGA Revival palette (GameUiTheme.tres) so combat stops being its own
// separate colour world.
public partial class CombatantMarkerPin : Button
{
	private static readonly Color AllyColor = new(0.28235295f, 0.7529412f, 0.84705883f);
	private static readonly Color EnemyColor = new(0.8784314f, 0.34117648f, 0.29803923f);
	private static readonly Color ActiveOutlineColor = new(0.9411765f, 0.7058824f, 0.16078432f);
	private static readonly Color HealthTrackColor = new(0.05490196f, 0.07058824f, 0.1254902f);

	// The medieval-heroes pack's _MVsv_alt_*.png strips are three
	// 128x128 frames side by side; only the first is used for a static
	// portrait (no animation wiring yet).
	private const float PortraitFrameSize = 128f;

	// Reserved along the bottom of the tile for the health bar and the
	// side strip, so the portrait never draws over either.
	private const float StatusStripHeight = 9f;
	private const float SideStripHeight = 2f;
	private const float HealthBarHeight = 3f;

	private Color _sideColor = AllyColor;
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
		bool isAlly,
		bool active,
		bool selected,
		int? currentHitPoints = null,
		int? maximumHitPoints = null,
		Texture2D? portrait = null,
		bool flipPortrait = false)
	{
		TooltipText = label;
		_sideColor = isAlly ? AllyColor : EnemyColor;
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
		Rect2 bounds = new(Vector2.Zero, Size);

		// Selection is a wash behind the token rather than another
		// outline, so it can coexist with the active outline without the
		// two competing for the same edge.
		if (_selected)
		{
			DrawRect(bounds, new Color(_sideColor, 0.20f));
		}

		if (_portrait is Texture2D portrait)
		{
			DrawPortrait(portrait);
		}

		DrawStatusStrips();

		if (_active)
		{
			DrawRect(bounds, ActiveOutlineColor, filled: false, width: 2f);
		}
	}

	// Draws only the first of the portrait's three 128x128 idle frames --
	// a static token image, not an animation. Fitted to the tile above
	// the status strips and drawn before them so nothing overlaps.
	//
	// No per-direction art exists (single static frame, see above), so
	// facing the other way means literally mirroring the draw call.
	// _flipPortrait is computed by the caller (CombatView.Markers.cs's
	// ShouldFlipPortrait, which also owns the "which side's art faces
	// which way by default" convention -- this class only knows how to
	// mirror, not why). A temporary scale.x=-1 transform, centered on
	// the portrait so the mirror axis is its own middle rather than the
	// control's corner, reset to identity immediately after so it can't
	// leak into the strip draws that follow.
	private void DrawPortrait(Texture2D portrait)
	{
		float available = Mathf.Max(Size.Y - StatusStripHeight, 1f);
		float portraitSize = Mathf.Min(Size.X, available);
		Vector2 center = new(Size.X / 2f, available / 2f);

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

	// Side strip always; health bar only when the caller supplied hit
	// points (mock combat content never has them).
	private void DrawStatusStrips()
	{
		float sideTop = Size.Y - SideStripHeight;
		DrawRect(new Rect2(0f, sideTop, Size.X, SideStripHeight), _sideColor);

		if (_currentHitPoints is not int current ||
			_maximumHitPoints is not int maximum ||
			maximum <= 0)
		{
			return;
		}

		float fraction = Mathf.Clamp((float)current / maximum, 0f, 1f);
		float barWidth = Size.X * 0.78f;
		float barLeft = (Size.X - barWidth) / 2f;
		float barTop = sideTop - HealthBarHeight - 1f;

		DrawRect(new Rect2(barLeft, barTop, barWidth, HealthBarHeight), HealthTrackColor);

		if (fraction <= 0f)
		{
			return;
		}

		Color healthColor = fraction switch
		{
			>= 0.5f => new Color(0.35686275f, 0.76862746f, 0.41568628f),
			>= 0.25f => new Color(0.9411765f, 0.7058824f, 0.16078432f),
			_ => new Color(0.8784314f, 0.34117648f, 0.29803923f),
		};

		DrawRect(
			new Rect2(barLeft, barTop, barWidth * fraction, HealthBarHeight),
			healthColor);
	}
}
