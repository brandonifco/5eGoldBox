using Godot;

// Rudimentary procedural per-cell overlays -- no image assets, no
// tileset/atlas pipeline (CLAUDE.md's Phase G note flags that as
// separate, larger work once flat colors prove insufficient, which the
// user has now said they do for testing purposes). Each cell already
// gets a flat base fill in AreaMapView.DrawGrid; these draw a small
// shape on top of it so floor/wall/door/stairs/treasure read as
// distinct kinds of thing at a glance, not just distinct colors.
public partial class AreaMapView
{
	private void DrawWallHatch(Rect2 cellRect, Color hatchColor)
	{
		float size = cellRect.Size.X;
		Vector2 origin = cellRect.Position;

		for (int i = 1; i <= 3; i++)
		{
			float frac = i / 4f;

			_gridLayer.DrawLine(
				origin + new Vector2(0, size * frac),
				origin + new Vector2(size * frac, 0),
				hatchColor,
				1.5f);

			_gridLayer.DrawLine(
				origin + new Vector2(size * frac, size),
				origin + new Vector2(size, size * frac),
				hatchColor,
				1.5f);
		}
	}

	private void DrawStairChevrons(Rect2 cellRect, Color arrowColor)
	{
		float size = cellRect.Size.X;
		Vector2 center = cellRect.Position + new Vector2(size / 2f, size / 2f);
		float armX = size * 0.28f;
		float armY = size * 0.16f;

		for (int i = 0; i < 2; i++)
		{
			float yOffset = (i - 0.5f) * size * 0.28f;
			Vector2 apex = center + new Vector2(0, yOffset - (armY / 2f));
			Vector2 left = center + new Vector2(-armX, yOffset + (armY / 2f));
			Vector2 right = center + new Vector2(armX, yOffset + (armY / 2f));

			_gridLayer.DrawLine(left, apex, arrowColor, 3f);
			_gridLayer.DrawLine(apex, right, arrowColor, 3f);
		}
	}

	private void DrawDoorLeaf(Rect2 cellRect, Color leafColor, Color knobColor)
	{
		float size = cellRect.Size.X;
		float leafWidth = size * 0.55f;
		float leafHeight = size * 0.82f;
		Vector2 leafTopLeft = cellRect.Position + new Vector2(
			(size - leafWidth) / 2f,
			(size - leafHeight) / 2f);

		_gridLayer.DrawRect(new Rect2(leafTopLeft, new Vector2(leafWidth, leafHeight)), leafColor);

		Vector2 knobCenter = leafTopLeft + new Vector2(leafWidth * 0.78f, leafHeight * 0.52f);
		_gridLayer.DrawCircle(knobCenter, size * 0.045f, knobColor);
	}

	private void DrawPadlock(Rect2 cellRect, Color bodyColor, Color shackleColor)
	{
		float size = cellRect.Size.X;
		Vector2 center = cellRect.Position + new Vector2(size / 2f, size * 0.3f);
		float bodyWidth = size * 0.2f;
		float bodyHeight = size * 0.15f;

		_gridLayer.DrawArc(
			center - new Vector2(0, bodyHeight * 0.15f),
			bodyWidth * 0.45f,
			Mathf.Pi,
			Mathf.Tau,
			12,
			shackleColor,
			2.5f);

		_gridLayer.DrawRect(
			new Rect2(center - new Vector2(bodyWidth / 2f, 0), new Vector2(bodyWidth, bodyHeight)),
			bodyColor);
	}

	private void DrawChest(Rect2 cellRect, Color bodyColor, Color lidColor, Color lockColor)
	{
		float size = cellRect.Size.X;
		float chestWidth = size * 0.6f;
		float bodyHeight = size * 0.28f;
		float lidHeight = size * 0.14f;
		Vector2 bodyTopLeft = cellRect.Position + new Vector2((size - chestWidth) / 2f, size * 0.52f);

		_gridLayer.DrawRect(new Rect2(bodyTopLeft, new Vector2(chestWidth, bodyHeight)), bodyColor);
		_gridLayer.DrawRect(
			new Rect2(bodyTopLeft - new Vector2(0, lidHeight), new Vector2(chestWidth, lidHeight)),
			lidColor);

		Vector2 lockCenter = bodyTopLeft + new Vector2(chestWidth / 2f, 0);
		_gridLayer.DrawCircle(lockCenter, size * 0.045f, lockColor);
	}
}
