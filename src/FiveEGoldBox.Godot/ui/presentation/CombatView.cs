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
	private ColorRect _placeholderBackground = null!;
	private TextureRect _combatImage = null!;
	private Control _floorLayer = null!;
	private Control _gridOverlay = null!;
	private Control _highlightsLayer = null!;
	private Control _combatantsLayer = null!;
	private Control _resultBanner = null!;
	private Label _resultLabel = null!;

	private readonly List<CombatantMarkerPin> _combatantPins = new();
	private readonly List<CombatHighlightCell> _highlightCells = new();

	private int _gridWidth = 1;
	private int _gridHeight = 1;
	private IReadOnlyList<CombatantMarkerViewModel> _combatants =
		Array.Empty<CombatantMarkerViewModel>();
	private IReadOnlyList<CombatHighlightViewModel> _highlights =
		Array.Empty<CombatHighlightViewModel>();
	private int _zoomIndex;
	private Vector2 _panOffset;
	private Texture2D? _floorTileTexture;
	private Vector2I? _hoveredCell;

	// M8e: fires when the player targets a combatant (marker click/Enter)
	// or a highlighted destination cell — the interaction controller
	// decides what either one means for the currently active command.
	public event Action<string>? CombatantActivated;
	public event Action<int, int>? CellActivated;

	public override void _Ready()
	{
		_combatViewport = GetNode<Control>("%CombatViewport");
		_combatContent = GetNode<Control>("%CombatContent");
		_placeholderBackground = GetNode<ColorRect>("%PlaceholderBackground");
		_combatImage = GetNode<TextureRect>("%CombatImage");
		_floorLayer = GetNode<Control>("%FloorLayer");
		_gridOverlay = GetNode<Control>("%GridOverlay");
		_highlightsLayer = GetNode<Control>("%HighlightsLayer");
		_combatantsLayer = GetNode<Control>("%CombatantsLayer");
		_resultBanner = GetNode<Control>("%ResultBanner");
		_resultLabel = GetNode<Label>("%ResultLabel");

		_floorLayer.Draw += DrawFloorTiles;
		_gridOverlay.Draw += DrawGridLines;
		_combatImage.Resized += RepositionAll;
		_combatViewport.GuiInput += OnCombatViewportGuiInput;
	}

	// Full (re)configure — only for a fresh combat start, so zoom resets
	// to a clean default exactly once, the same way
	// ShellPresentationController.ShowExploration always resets facing.
	// Everything else (a highlight change, a target selection, a move, a
	// turn advance) goes through Refresh instead, which leaves the
	// player's current zoom alone.
	internal void Configure(CombatViewModel model)
	{
		_zoomIndex = 0;
		Refresh(model);
		// Refresh alone only clamps whatever pan offset already existed
		// (see CombatView.Zoom.cs's own reasoning) — a fresh encounter
		// still wants to start centered on the opening focus, the same
		// "reset to a clean default exactly once" this method's own
		// zoom reset already does.
		CenterPanOnFocus();
	}

	internal void Refresh(CombatViewModel model)
	{
		_gridWidth = model.GridWidth;
		_gridHeight = model.GridHeight;
		_combatants = model.Combatants;
		_highlights = model.Highlights ?? Array.Empty<CombatHighlightViewModel>();

		_combatImage.Visible = model.HasArtBackground;
		_placeholderBackground.Visible = !model.HasArtBackground;
		_floorTileTexture = ResolveFloorTileTexture(model.FloorTileSheetPath);

		RebuildCombatants();
		RebuildHighlights();
		// Not a recenter — a turn advancing or a highlight changing
		// shouldn't yank the view back to center every time; only
		// _Process's own edge auto-scroll (CombatView.Zoom.cs) moves the
		// pan offset once a real encounter is underway. This just keeps
		// whatever offset already exists valid against the current zoom/
		// grid, e.g. after a resize.
		ClampPanOffset();
		_floorLayer.QueueRedraw();
		_gridOverlay.QueueRedraw();
		UpdateResultBanner(model.InformationalOverlays);
	}

	// Same GD.Load resource-cache reasoning as CombatView.Markers.cs's
	// own ResolvePortrait.
	private static Texture2D? ResolveFloorTileTexture(string? sheetPath)
	{
		return sheetPath is null
			? null
			: GD.Load<Texture2D>(sheetPath);
	}

	// M8f: "completed-combat presentation states" — the one thing
	// CombatViewModel.InformationalOverlays was already carrying
	// (M5c's own CompletedEncounterDisplay scenario) with nothing on the
	// live view ever reading it until now.
	private void UpdateResultBanner(IReadOnlyList<string>? informationalOverlays)
	{
		bool hasOverlays = informationalOverlays is { Count: > 0 };

		_resultBanner.Visible = hasOverlays;
		_resultLabel.Text = hasOverlays
			? string.Join("\n", informationalOverlays!)
			: string.Empty;
	}

	private void RepositionAll()
	{
		ClampPanOffset();
		RepositionCombatants();
		RepositionHighlights();
		_floorLayer.QueueRedraw();
		_gridOverlay.QueueRedraw();
	}
}
