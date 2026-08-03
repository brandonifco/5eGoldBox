using Godot;

// Rudimentary procedural per-cell overlays -- no image assets, no
// tileset/atlas pipeline (CLAUDE.md's Phase G note flags that as
// separate, larger work once flat colors prove insufficient). Restyled
// to read as a hand-drawn dungeon-cartography map (thick black ink
// perimeter, cross-hatched stone fill, gap-and-leaf door symbols)
// rather than flat-colored cells, per the reference the user supplied --
// each cell still gets a base fill in AreaMapView.DrawGrid; these draw a
// small shape/texture on top so floor/wall/door/stairs/treasure read as
// distinct kinds of thing at a glance, not just distinct colors.
public partial class AreaMapView
{
	// A dense diagonal cross-hatch approximating the scratchy stone
	// texture packed dungeon-map rock fills use -- two crossing sets of
	// short diagonal strokes rather than one, so the cell reads as
	// "solid rock" instead of a single hatch direction.
	private void DrawStoneHatch(Rect2 cellRect, Color hatchColor)
	{
		float size = cellRect.Size.X;
		Vector2 origin = cellRect.Position;

		for (int i = 1; i <= 4; i++)
		{
			float frac = i / 5f;

			_gridLayer.DrawLine(
				origin + new Vector2(0, size * frac),
				origin + new Vector2(size * frac, 0),
				hatchColor,
				1.25f);

			_gridLayer.DrawLine(
				origin + new Vector2(size * frac, size),
				origin + new Vector2(size, size * frac),
				hatchColor,
				1.25f);
		}
	}

	private void DrawStairChevrons(Rect2 cellRect, Color inkColor)
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

			_gridLayer.DrawLine(left, apex, inkColor, 2.5f);
			_gridLayer.DrawLine(apex, right, inkColor, 2.5f);
		}
	}

	// Outline-only, no color fill -- a chest drawn as plain ink linework
	// like everything else on the map, with just the lock kept as a
	// small gold accent dot.
	private void DrawChest(Rect2 cellRect, Color inkColor, Color lockColor)
	{
		float size = cellRect.Size.X;
		float chestWidth = size * 0.6f;
		float bodyHeight = size * 0.28f;
		float lidHeight = size * 0.14f;
		Vector2 bodyTopLeft = cellRect.Position + new Vector2((size - chestWidth) / 2f, size * 0.52f);

		Rect2 bodyRect = new(bodyTopLeft, new Vector2(chestWidth, bodyHeight));
		Rect2 lidRect = new(
			bodyTopLeft - new Vector2(0, lidHeight),
			new Vector2(chestWidth, lidHeight));

		_gridLayer.DrawRect(bodyRect, inkColor, filled: false, width: size * 0.035f);
		_gridLayer.DrawRect(lidRect, inkColor, filled: false, width: size * 0.035f);

		Vector2 lockCenter = bodyTopLeft + new Vector2(chestWidth / 2f, 0);
		_gridLayer.DrawCircle(lockCenter, size * 0.045f, lockColor);
	}

	// Draws one boundary segment between two grid cells -- either a solid
	// ink wall line (no door), or a gap-with-leaf door symbol: the wall
	// line breaks for the middle third of the edge, and a short tick
	// straddling the gap's center stands in for the door leaf itself,
	// matching the reference map's convention. An open door leaves the
	// gap bare (no tick) so it reads as a passable opening; a locked one
	// keeps the tick and adds a small dot accent.
	private void DrawWallOrDoorSegment(
		Vector2 start,
		Vector2 end,
		Color inkColor,
		Color lockedAccentColor,
		float wallWidth,
		(bool IsLocked, bool IsOpen)? door)
	{
		if (door is null)
		{
			_gridLayer.DrawLine(start, end, inkColor, wallWidth);
			return;
		}

		Vector2 direction = end - start;
		Vector2 gapStart = start + (direction * 0.32f);
		Vector2 gapEnd = start + (direction * 0.68f);

		_gridLayer.DrawLine(start, gapStart, inkColor, wallWidth);
		_gridLayer.DrawLine(gapEnd, end, inkColor, wallWidth);

		if (door.Value.IsOpen)
		{
			return;
		}

		Vector2 mid = (gapStart + gapEnd) / 2f;
		Vector2 perpendicular = new Vector2(-direction.Y, direction.X).Normalized()
			* (direction.Length() * 0.22f);
		Color leafColor = door.Value.IsLocked ? lockedAccentColor : inkColor;

		_gridLayer.DrawLine(mid - perpendicular, mid + perpendicular, leafColor, wallWidth * 0.75f);

		if (door.Value.IsLocked)
		{
			_gridLayer.DrawCircle(mid, wallWidth * 1.1f, lockedAccentColor);
		}
	}
}
