using Godot;

// A real 3D first-person dungeon view, rendering every real floor cell
// and wall/door boundary ExplorationView.ResolveCorridor floods outward
// from the party's position -- not just a single straight lane ahead,
// so a branch, an open room, or a door beside an open passage all
// render as themselves instead of empty margin or disconnected wall
// fragments. Earlier versions of this file tried both a hand-composited
// 2D sprite layering technique and a straight-line-only 3D corridor;
// see git history on this file and on ExplorationView.CorridorResolution.cs
// for that account.
//
// Renders into an embedded SubViewport (this Control IS the
// SubViewportContainer) with a fixed camera at local origin looking
// down -Z; geometry is rebuilt from explicit vertex positions every
// Configure call rather than the party's real facing rotating anything,
// since the resolver already expresses every cell in camera-relative
// forward/lateral grid units.
//
// Script-only, no .tscn, same convention PartyDirectionMarker/
// CombatHighlightCell use, extended here to also build its own child
// SubViewport/Camera3D/geometry-root at runtime.
internal sealed partial class DungeonCorridor3DView : SubViewportContainer
{
	// World units per grid cell, and the corridor's wall height. Chosen
	// to read as a normal-proportioned corridor at a normal FOV, not
	// derived from any real-world scale.
	private const float CellSize = 2f;
	private const float WallHeight = 2.2f;
	private const float HalfWidth = CellSize / 2f;
	private const float HalfHeight = WallHeight / 2f;

	// Manhattan-distance flood-fill radius (in cells) from the party's
	// own position. Generous relative to every current scenario's small
	// maps (a handful of tiles per floor), so this comfortably covers
	// an entire floor rather than clipping a real room.
	private const int Radius = 8;

	// Deliberately well above black so a dimly-lit far cell is still
	// clearly "a surface" rather than reading as unrendered void once
	// screenshot compression flattens subtle dark tones.
	private static readonly Color FloorColor = new(0.5f, 0.45f, 0.38f, 1f);
	private static readonly Color CeilingColor = new(0.34f, 0.31f, 0.27f, 1f);

	private SubViewport _viewport = null!;
	private Node3D _geometryRoot = null!;

	internal static int ConfigureRadius => Radius;

	public override void _Ready()
	{
		Stretch = true;
		MouseFilter = MouseFilterEnum.Ignore;

		_viewport = new SubViewport
		{
			Size = new Vector2I(512, 512),
			TransparentBg = true,
		};
		AddChild(_viewport);

		Camera3D camera = new()
		{
			Fov = 75f,
			Near = 0.05f,
			Far = CellSize * (Radius + 2),
		};
		_viewport.AddChild(camera);
		// Godot does not reliably auto-promote a lone camera to active
		// -- Current must be requested explicitly, and only works once
		// the camera is inside the scene tree, hence set after AddChild.
		camera.Current = true;

		_geometryRoot = new Node3D();
		_viewport.AddChild(_geometryRoot);
	}

	internal void Configure(
		ExplorationView.CorridorGeometry geometry,
		DungeonWallMaterial material)
	{
		ClearGeometry();

		foreach (ExplorationView.CorridorFloorCell cell in geometry.FloorCells)
		{
			Color tint = DistanceTint(cell.Forward, cell.Lateral);
			float zNear = -cell.Forward * CellSize;
			float zFar = -(cell.Forward + 1) * CellSize;
			float xCenter = cell.Lateral * CellSize;

			AddFace(
				FloorCorners(xCenter, zNear, zFar),
				FlatMaterial(FloorColor * tint));
			AddFace(
				CeilingCorners(xCenter, zNear, zFar),
				FlatMaterial(CeilingColor * tint));
		}

		foreach (ExplorationView.CorridorWallSegment segment in geometry.WallSegments)
		{
			Color tint = DistanceTint(segment.Forward, segment.Lateral);
			float zNear = -segment.Forward * CellSize;
			float zFar = -(segment.Forward + 1) * CellSize;
			float xCenter = segment.Lateral * CellSize;
			Texture2D texture = segment.Kind == ExplorationView.CorridorCellKind.Door
				? material.DoorTexture
				: material.WallTexture;

			AddFace(
				WallCorners(segment.Side, xCenter, zNear, zFar),
				TexturedMaterial(texture, tint));
		}
	}

	internal void Clear()
	{
		ClearGeometry();
	}

	private void ClearGeometry()
	{
		foreach (Node child in _geometryRoot.GetChildren())
		{
			child.QueueFree();
		}
	}

	private static Color DistanceTint(int forward, int lateral)
	{
		int distance = Mathf.Abs(forward) + Mathf.Abs(lateral);
		float brightness = Mathf.Max(1f - (0.08f * distance), 0.35f);

		return new Color(brightness, brightness, brightness, 1f);
	}

	private static Vector3[] FloorCorners(float xCenter, float zNear, float zFar)
	{
		float xLeft = xCenter - HalfWidth;
		float xRight = xCenter + HalfWidth;

		return new Vector3[]
		{
			new(xLeft, -HalfHeight, zNear),
			new(xRight, -HalfHeight, zNear),
			new(xRight, -HalfHeight, zFar),
			new(xLeft, -HalfHeight, zFar),
		};
	}

	private static Vector3[] CeilingCorners(float xCenter, float zNear, float zFar)
	{
		float xLeft = xCenter - HalfWidth;
		float xRight = xCenter + HalfWidth;

		return new Vector3[]
		{
			new(xLeft, HalfHeight, zNear),
			new(xRight, HalfHeight, zNear),
			new(xRight, HalfHeight, zFar),
			new(xLeft, HalfHeight, zFar),
		};
	}

	// One cell's own edge, in one of the four camera-relative
	// directions -- Forward/Backward are the near/far edges (spanning
	// the cell's own width), Left/Right are the side edges (spanning
	// the cell's own depth).
	private static Vector3[] WallCorners(
		ExplorationView.CorridorEdgeSide side,
		float xCenter,
		float zNear,
		float zFar)
	{
		float xLeft = xCenter - HalfWidth;
		float xRight = xCenter + HalfWidth;

		return side switch
		{
			ExplorationView.CorridorEdgeSide.Forward => new Vector3[]
			{
				new(xLeft, HalfHeight, zFar),
				new(xRight, HalfHeight, zFar),
				new(xRight, -HalfHeight, zFar),
				new(xLeft, -HalfHeight, zFar),
			},
			ExplorationView.CorridorEdgeSide.Backward => new Vector3[]
			{
				new(xLeft, HalfHeight, zNear),
				new(xRight, HalfHeight, zNear),
				new(xRight, -HalfHeight, zNear),
				new(xLeft, -HalfHeight, zNear),
			},
			ExplorationView.CorridorEdgeSide.Left => new Vector3[]
			{
				new(xLeft, HalfHeight, zNear),
				new(xLeft, HalfHeight, zFar),
				new(xLeft, -HalfHeight, zFar),
				new(xLeft, -HalfHeight, zNear),
			},
			_ => new Vector3[] // Right
			{
				new(xRight, HalfHeight, zFar),
				new(xRight, HalfHeight, zNear),
				new(xRight, -HalfHeight, zNear),
				new(xRight, -HalfHeight, zFar),
			},
		};
	}

	// corners must be four coplanar points in order (either winding --
	// CullDisabled below means both directions render, so a mistaken
	// winding costs only texture mirroring, never an invisible face).
	private void AddFace(Vector3[] corners, StandardMaterial3D material)
	{
		SurfaceTool st = new();

		st.Begin(Mesh.PrimitiveType.Triangles);
		st.SetMaterial(material);

		AddVertex(st, corners[0], new Vector2(0, 0));
		AddVertex(st, corners[1], new Vector2(1, 0));
		AddVertex(st, corners[2], new Vector2(1, 1));

		AddVertex(st, corners[0], new Vector2(0, 0));
		AddVertex(st, corners[2], new Vector2(1, 1));
		AddVertex(st, corners[3], new Vector2(0, 1));

		ArrayMesh mesh = st.Commit();
		MeshInstance3D instance = new() { Mesh = mesh };

		_geometryRoot.AddChild(instance);
	}

	private static void AddVertex(SurfaceTool st, Vector3 position, Vector2 uv)
	{
		st.SetUV(uv);
		st.AddVertex(position);
	}

	private static StandardMaterial3D FlatMaterial(Color color)
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoColor = color,
		};
	}

	private static StandardMaterial3D TexturedMaterial(Texture2D texture, Color tint)
	{
		return new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			AlbedoTexture = texture,
			AlbedoColor = tint,
		};
	}
}
