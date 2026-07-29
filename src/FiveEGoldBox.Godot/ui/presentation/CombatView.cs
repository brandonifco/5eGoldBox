using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// Split into partials by concern from the start this time (M8a) — the
// same 250-line pattern that RegionalMapView only adopted after
// crossing the threshold (M7b) applies just as much here, and this view
// has at least as much going on (grid, markers, highlights, zoom/pan).
// This file keeps node wiring and (re)configure; grid/marker/highlight
// drawing is in CombatView.Markers.cs, zoom/pan camera math is in
// CombatView.Zoom.cs.
public partial class CombatView : Control
{
	private Control _combatViewport = null!;
	private Control _combatContent = null!;
	private TextureRect _combatImage = null!;
	private Control _gridOverlay = null!;
	private Control _highlightsLayer = null!;
	private Control _combatantsLayer = null!;

	private readonly List<CombatantMarkerPin> _combatantPins = new();
	private readonly List<CombatHighlightCell> _highlightCells = new();

	private int _gridWidth = 1;
	private int _gridHeight = 1;
	private IReadOnlyList<CombatantMarkerViewModel> _combatants =
		Array.Empty<CombatantMarkerViewModel>();
	private IReadOnlyList<CombatHighlightViewModel> _highlights =
		Array.Empty<CombatHighlightViewModel>();
	private int _zoomIndex;

	// M8e: fires when the player targets a combatant (marker click/Enter)
	// or a highlighted destination cell — the interaction controller
	// decides what either one means for the currently active command.
	public event Action<string>? CombatantActivated;
	public event Action<int, int>? CellActivated;

	public override void _Ready()
	{
		_combatViewport = GetNode<Control>("%CombatViewport");
		_combatContent = GetNode<Control>("%CombatContent");
		_combatImage = GetNode<TextureRect>("%CombatImage");
		_gridOverlay = GetNode<Control>("%GridOverlay");
		_highlightsLayer = GetNode<Control>("%HighlightsLayer");
		_combatantsLayer = GetNode<Control>("%CombatantsLayer");

		_gridOverlay.Draw += DrawGridLines;
		_combatImage.Resized += RepositionAll;
		_combatViewport.GuiInput += OnCombatViewportGuiInput;
	}

	internal void Configure(CombatViewModel model)
	{
		_gridWidth = model.GridWidth;
		_gridHeight = model.GridHeight;
		_combatants = model.Combatants;
		_highlights = model.Highlights ?? Array.Empty<CombatHighlightViewModel>();
		_zoomIndex = 0;

		RebuildCombatants();
		RebuildHighlights();
		ApplyZoomAndPan();
		_gridOverlay.QueueRedraw();
	}

	private void RepositionAll()
	{
		RepositionCombatants();
		RepositionHighlights();
		_gridOverlay.QueueRedraw();
	}
}
