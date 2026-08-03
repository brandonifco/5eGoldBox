using Godot;

// The party's real position/facing on the area map — script-only, mirrors
// CombatHighlightCell/CombatantMarkerPin's "no .tscn, draw in _Draw()"
// convention. Always drawn as one triangle pointing "up" in local space;
// facing is expressed by rotating the whole node around its own center
// (AreaMapView sets PivotOffset to Size/2 whenever it repositions this),
// not by recomputing triangle points per direction.
public partial class PartyDirectionMarker : Control
{
	private static readonly Color FillColor = new(0.72f, 0.12f, 0.1f, 0.95f);
	private static readonly Color OutlineColor = new(0.08f, 0.07f, 0.06f, 1f);

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
	}

	public void Configure(string facing)
	{
		RotationDegrees = facing switch
		{
			"North" => 0f,
			"East" => 90f,
			"South" => 180f,
			"West" => 270f,
			_ => 0f,
		};

		if (IsNodeReady())
		{
			QueueRedraw();
		}
	}

	public override void _Draw()
	{
		Vector2 size = Size;
		Vector2[] points =
		{
			new(size.X / 2f, 0f),
			new(size.X, size.Y),
			new(0f, size.Y),
		};

		DrawColoredPolygon(points, FillColor);
		DrawPolyline(new[] { points[0], points[1], points[2], points[0] }, OutlineColor, 1.5f);
	}
}
