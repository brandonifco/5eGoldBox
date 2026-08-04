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
	// Temporary diagnostic overlay -- the camera math (focus point, pan
	// clamp bounds, viewport size) has no other way to inspect what it
	// actually computed short of screenshots and guessing, the same
	// reason AreaMapView grew its own "(X, Y) Facing" debug overlay while
	// diagnosing the 3D corridor. Left in rather than stripped out once
	// the current camera bug is found, on the same reasoning.
	private Label _debugCameraLabel = null!;

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
	// Whichever highlight cell (see CombatHighlightCell) currently has
	// keyboard/mouse focus -- set from RebuildHighlights' own
	// FocusEntered wiring, read by ApplyEdgeAutoScroll (CombatView.
	// Zoom.cs) so arrow-key cursor movement can scroll the view near an
	// edge the same way mouse-hover already does, not just recenter once
	// per turn. Cleared at the start of every RebuildHighlights, since
	// the old cell it might have pointed at is about to be freed.
	private Vector2I? _cursorFocusedCell;
	// Tracks whose turn the camera last centered on, so Refresh can tell
	// "still the same combatant's turn" (leave the player's own scrolling
	// alone) apart from "turn just advanced to someone else" (recenter).
	// Starts null, which itself counts as "different" the first time
	// Refresh runs for a fresh encounter.
	private string? _lastCenteredCombatantId;

	// M8e: fires when the player targets a combatant (marker click/Enter)
	// or a highlighted destination cell — the interaction controller
	// decides what either one means for the currently active command.
	public event Action<string>? CombatantActivated;
	public event Action<int, int>? CellActivated;
	// Fires whenever a "move-legal"/"move-illegal" cursor cell (see
	// CombatHighlightCell) gains keyboard/mouse focus — the interaction
	// controller already knows which positions are legal from the same
	// move-destination data it built the cursor grid from, so this only
	// needs to report where the cursor is now, not judge it itself.
	public event Action<int, int>? CellCursorFocused;

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
		_debugCameraLabel = GetNode<Label>("%DebugCameraLabel");

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
		// Forces Refresh's own "did the active combatant change" check
		// below to read as true on this first call, so a fresh encounter
		// centers on the opening turn the same way any later turn change
		// does — one recenter rule instead of two.
		_lastCenteredCombatantId = null;
		Refresh(model);
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

		// Recenter exactly when whose turn it is changes — a fresh
		// encounter (Configure resets _lastCenteredCombatantId to null
		// first) or an ordinary turn advance both read as "different"
		// here. A target selection or a move that doesn't end the turn
		// leaves ActiveCombatantId alone, so the camera stays put for
		// those, matching what the user actually asked for: center on
		// whoever has initiative, not on every highlight change.
		if (model.ActiveCombatantId != _lastCenteredCombatantId)
		{
			_lastCenteredCombatantId = model.ActiveCombatantId;
			CenterPanOnFocus();
		}
		else
		{
			// Keeps whatever offset already exists valid against the
			// current zoom/grid (e.g. after a resize) without moving it.
			ClampPanOffset();
		}

		_floorLayer.QueueRedraw();
		_gridOverlay.QueueRedraw();
		UpdateResultBanner(model.InformationalOverlays);
		UpdateDebugCameraLabel();
	}

	// See _debugCameraLabel's own field comment. Also called every
	// _Process frame (CombatView.Zoom.cs) so mouse-hover edge-scroll's
	// continuous pan changes show up live, not just at the start/end of
	// each action. Derives the active combatant from _combatants itself
	// rather than taking a model param, so both call sites can share it.
	private void UpdateDebugCameraLabel()
	{
		CombatantMarkerViewModel? active = _combatants
			.FirstOrDefault(combatant => combatant.Active);
		IsoMetrics metrics = Metrics;

		_debugCameraLabel.Text =
			$"active={active?.Id ?? "(none)"} " +
			$"grid=({active?.GridX.ToString() ?? "?"},{active?.GridY.ToString() ?? "?"}) " +
			$"lastCentered={_lastCenteredCombatantId ?? "(none)"}\n" +
			$"viewport={_combatViewport.Size} image={_combatImage.Size}\n" +
			$"focus={ResolveFocusPoint()} pan={_panOffset} zoom={ZoomLevels[_zoomIndex]}\n" +
			$"diamond=({metrics.DiamondWidth:F0},{metrics.DiamondHeight:F0}) tile=({metrics.TileWidth:F0},{metrics.TileHeight:F0})";
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
