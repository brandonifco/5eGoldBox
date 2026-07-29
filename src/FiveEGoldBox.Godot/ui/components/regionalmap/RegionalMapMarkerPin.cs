using Godot;

// A passive, custom-drawn visual pin for one point on the regional map —
// script-only (no .tscn) since it has no child nodes to declare, the same
// way ShellCommandBarController.RenderMovementPrompt builds a plain
// RichTextLabel directly in code rather than via a scene file. Purely
// reactive: RegionalMapView owns position/selection state and calls
// Configure; this never reads input itself (mouse selection goes through
// SelectionList, not these pins) — MouseFilter is Ignore for that reason.
public partial class RegionalMapMarkerPin : Control
{
	public enum PinKind
	{
		Location,
		Party,
	}

	private const float LocationDiameter = 18f;
	private const float SelectedDiameter = 26f;
	private const float PartyDiameter = 24f;

	private PinKind _kind = PinKind.Location;
	private bool _selected;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public float Diameter => _kind == PinKind.Party
		? PartyDiameter
		: _selected ? SelectedDiameter : LocationDiameter;

	public void Configure(PinKind kind, bool selected)
	{
		_kind = kind;
		_selected = selected;
		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 center = Size / 2f;
		float radius = Mathf.Min(Size.X, Size.Y) / 2f;
		(Color fill, Color border, float borderWidth) = ResolveStyle();

		DrawCircle(center, radius, fill);
		DrawArc(
			center,
			radius,
			0,
			Mathf.Tau,
			32,
			border,
			borderWidth,
			antialiased: true);
	}

	private (Color Fill, Color Border, float BorderWidth) ResolveStyle()
	{
		if (_kind == PinKind.Party)
		{
			return (
				new Color(0.36f, 0.62f, 0.86f, 0.95f),
				new Color(0.88f, 0.92f, 0.96f, 1f),
				2f);
		}

		if (_selected)
		{
			// Same gold used for focus/selection everywhere else in this
			// theme (StyleBoxFlat_command_button_focus/_selected_row) —
			// not a new accent color.
			return (
				new Color(0.9254902f, 0.78039217f, 0.36078432f, 0.85f),
				new Color(0.9254902f, 0.78039217f, 0.36078432f, 1f),
				2.5f);
		}

		return (
			new Color(0.09f, 0.1f, 0.12f, 0.55f),
			new Color(0.9f, 0.9f, 0.92f, 0.85f),
			1.5f);
	}
}
