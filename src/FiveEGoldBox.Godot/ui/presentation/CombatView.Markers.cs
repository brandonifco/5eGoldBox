using System.Linq;
using Godot;

// Grid lines, combatant markers, and highlight cells — split out of
// CombatView.cs (see that file's header).
public partial class CombatView
{
	private Vector2 CellSize => new(
		_combatImage.Size.X / _gridWidth,
		_combatImage.Size.Y / _gridHeight);

	private void RebuildCombatants()
	{
		foreach (CombatantMarkerPin pin in _combatantPins)
		{
			_combatantsLayer.RemoveChild(pin);
			pin.QueueFree();
		}

		_combatantPins.Clear();

		int allyIndex = 0;

		foreach (CombatantMarkerViewModel combatant in _combatants)
		{
			CombatantMarkerPin pin = new();
			_combatantsLayer.AddChild(pin);
			pin.Configure(
				combatant.Label,
				allyIndex,
				combatant.IsAlly,
				combatant.Active,
				combatant.Selected);

			string combatantId = combatant.Id;
			pin.Pressed += () => CombatantActivated?.Invoke(combatantId);

			PositionCombatant(pin, combatant.GridX, combatant.GridY);
			_combatantPins.Add(pin);

			if (combatant.IsAlly)
			{
				allyIndex++;
			}
		}
	}

	private void RepositionCombatants()
	{
		for (int index = 0; index < _combatants.Count; index++)
		{
			CombatantMarkerViewModel combatant = _combatants[index];
			PositionCombatant(_combatantPins[index], combatant.GridX, combatant.GridY);
		}
	}

	private void PositionCombatant(CombatantMarkerPin pin, int gridX, int gridY)
	{
		Vector2 cellSize = CellSize;
		float diameter = Mathf.Min(cellSize.X, cellSize.Y) * 0.75f;
		Vector2 cellCenter = new(
			(gridX + 0.5f) * cellSize.X,
			(gridY + 0.5f) * cellSize.Y);

		pin.Size = new Vector2(diameter, diameter);
		pin.Position = cellCenter - pin.Size / 2f;
	}

	private void RebuildHighlights()
	{
		foreach (CombatHighlightCell cell in _highlightCells)
		{
			_highlightsLayer.RemoveChild(cell);
			cell.QueueFree();
		}

		_highlightCells.Clear();

		foreach (CombatHighlightViewModel highlight in _highlights)
		{
			CombatHighlightCell cell = new();
			_highlightsLayer.AddChild(cell);
			cell.Configure(highlight.Kind);

			int gridX = highlight.GridX;
			int gridY = highlight.GridY;
			cell.Pressed += () => CellActivated?.Invoke(gridX, gridY);

			PositionHighlight(cell, gridX, gridY);
			_highlightCells.Add(cell);
		}
	}

	private void RepositionHighlights()
	{
		for (int index = 0; index < _highlights.Count; index++)
		{
			CombatHighlightViewModel highlight = _highlights[index];
			PositionHighlight(_highlightCells[index], highlight.GridX, highlight.GridY);
		}
	}

	private void PositionHighlight(CombatHighlightCell cell, int gridX, int gridY)
	{
		Vector2 cellSize = CellSize;

		cell.Position = new Vector2(gridX * cellSize.X, gridY * cellSize.Y);
		cell.Size = cellSize;
	}

	// M8a: the "bird's-eye tactical grid" itself — faint lines only, so
	// the art underneath still reads as the scene it is.
	private void DrawGridLines()
	{
		Vector2 cellSize = CellSize;
		Vector2 imageSize = _combatImage.Size;
		Color lineColor = new(1f, 1f, 1f, 0.18f);

		for (int x = 0; x <= _gridWidth; x++)
		{
			float lineX = x * cellSize.X;
			_gridOverlay.DrawLine(
				new Vector2(lineX, 0),
				new Vector2(lineX, imageSize.Y),
				lineColor);
		}

		for (int y = 0; y <= _gridHeight; y++)
		{
			float lineY = y * cellSize.Y;
			_gridOverlay.DrawLine(
				new Vector2(0, lineY),
				new Vector2(imageSize.X, lineY),
				lineColor);
		}
	}
}
