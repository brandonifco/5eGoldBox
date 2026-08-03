using System.Collections.Generic;
using Godot;

// Renders ExplorationView.CorridorDepthLayer data as a classic first-
// person "blobber" dungeon corridor -- the same layered-perspective
// compositing technique Wizardry, Bard's Tale, Dungeon Master and Eye of
// the Beholder used, and the one Screaming Brain Studios' own "First
// Person Dungeons" tutorial documents for this exact asset pack
// (docs/2026-08-03-sbs-dungeon-tileset-inventory.md has the full
// citation). No real 3D: each depth is a flat Top/Bottom/Left/Right/
// Center frame at a fixed 0.25/0.5/0.25 proportion, and depth 2's whole
// frame is nested inside depth 1's Center slot -- which is exactly why
// the pack ships Layer 2 at precisely half the pixel dimensions of
// Layer 1's own Center piece. Script-only, no .tscn, same convention
// PartyDirectionMarker/CombatHighlightCell already use for this kind of
// small presentation-only component.
internal sealed partial class DungeonCorridorView : Control
{
	// Each depth's frame occupies this fraction of its own containing
	// square along each axis (Top/Bottom/Left/Right), with the remaining
	// center fraction holding either the terminating wall/door texture
	// or the next depth's own frame, nested. Verified against the
	// pack's own shipped pixel dimensions (256/64/128/64/256-style
	// splits at every depth), not chosen arbitrarily.
	private const float EdgeFraction = 0.25f;
	private const float CenterFraction = 0.5f;

	private static readonly Color NearModulate = new(1f, 1f, 1f, 1f);
	private static readonly Color FarModulate = new(0.5f, 0.5f, 0.5f, 1f);
	private static readonly Color UnseenColor = new(0.05f, 0.05f, 0.05f, 1f);

	private IReadOnlyList<ExplorationView.CorridorDepthLayer>? _layers;
	private DungeonWallMaterial? _material;

	public override void _Ready()
	{
		Resized += QueueRedraw;
	}

	internal void Configure(
		IReadOnlyList<ExplorationView.CorridorDepthLayer> layers,
		DungeonWallMaterial material)
	{
		_layers = layers;
		_material = material;
		QueueRedraw();
	}

	internal void Clear()
	{
		_layers = null;
		_material = null;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_layers is not { Count: > 0 } layers || _material is null)
		{
			return;
		}

		float side = Mathf.Min(Size.X, Size.Y);
		Vector2 canvasSize = new(side, side);
		Vector2 canvasOrigin = (Size - canvasSize) / 2f;

		DrawDepth(new Rect2(canvasOrigin, canvasSize), 1, layers, _material);
	}

	private void DrawDepth(
		Rect2 square,
		int depth,
		IReadOnlyList<ExplorationView.CorridorDepthLayer> layers,
		DungeonWallMaterial material)
	{
		if (depth > layers.Count)
		{
			// Open corridor beyond how far this material renders --
			// darkness, not a wall.
			DrawRect(square, UnseenColor);
			return;
		}

		ExplorationView.CorridorDepthLayer layer = layers[depth - 1];
		bool isNear = depth == 1;
		Color modulate = isNear ? NearModulate : FarModulate;
		Vector2 origin = square.Position;
		Vector2 size = square.Size;

		Rect2 topRect = new(
			origin,
			new Vector2(size.X, size.Y * EdgeFraction));
		Rect2 bottomRect = new(
			origin + new Vector2(0, size.Y * (1f - EdgeFraction)),
			new Vector2(size.X, size.Y * EdgeFraction));
		Rect2 leftRect = new(
			origin + new Vector2(0, size.Y * EdgeFraction),
			new Vector2(size.X * EdgeFraction, size.Y * CenterFraction));
		Rect2 rightRect = new(
			origin + new Vector2(size.X * (1f - EdgeFraction), size.Y * EdgeFraction),
			new Vector2(size.X * EdgeFraction, size.Y * CenterFraction));
		Rect2 centerRect = new(
			origin + (size * EdgeFraction),
			size * CenterFraction);

		Texture2D top = isNear ? material.Layer1Top : material.Layer2Top;
		Texture2D bottom = isNear ? material.Layer1Bottom : material.Layer2Bottom;
		Texture2D left = isNear ? material.Layer1Left : material.Layer2Left;
		Texture2D right = isNear ? material.Layer1Right : material.Layer2Right;
		Texture2D center = isNear ? material.Layer1Center : material.Layer2Center;
		Texture2D doorLeft = isNear ? material.Layer1DoorLeft : material.Layer2DoorLeft;
		Texture2D doorCenter = isNear ? material.Layer1DoorCenter : material.Layer2DoorCenter;
		Texture2D doorRight = isNear ? material.Layer1DoorRight : material.Layer2DoorRight;

		DrawTextureRect(top, topRect, false, modulate);
		DrawTextureRect(bottom, bottomRect, false, modulate);

		switch (layer.Left)
		{
			case ExplorationView.CorridorCellKind.Wall:
				DrawTextureRect(left, leftRect, false, modulate);
				break;
			case ExplorationView.CorridorCellKind.Door:
				DrawTextureRect(doorLeft, leftRect, false, modulate);
				break;
		}

		switch (layer.Right)
		{
			case ExplorationView.CorridorCellKind.Wall:
				DrawTextureRect(right, rightRect, false, modulate);
				break;
			case ExplorationView.CorridorCellKind.Door:
				DrawTextureRect(doorRight, rightRect, false, modulate);
				break;
		}

		switch (layer.Ahead)
		{
			case ExplorationView.CorridorCellKind.Wall:
				DrawTextureRect(center, centerRect, false, modulate);
				break;
			case ExplorationView.CorridorCellKind.Door:
				DrawTextureRect(doorCenter, centerRect, false, modulate);
				break;
			default:
				DrawDepth(centerRect, depth + 1, layers, material);
				break;
		}
	}
}
