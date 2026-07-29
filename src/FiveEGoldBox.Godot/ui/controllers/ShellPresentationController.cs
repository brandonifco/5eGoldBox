using System.Collections.Generic;
using Godot;

internal sealed class ShellPresentationController : IShellPresentation
{
	// M6b dev proof only: cycles ExplorationView through every scene key
	// that has a distinct appearance, real image first. Not "the" list of
	// exploration scenes — MockExplorationScenarios/MockScenarioCatalog
	// remain the actual catalog.
	private static readonly string[] ExplorationVariantSceneKeys =
	{
		ExplorationSceneKeys.OutpostEntrance,
		ExplorationSceneKeys.BuildingInterior,
		ExplorationSceneKeys.TownStreet,
		ExplorationSceneKeys.Cavern,
		ExplorationSceneKeys.DungeonCorridor,
	};

	private readonly ExplorationView _explorationView;
	private readonly RegionalMapView _regionalMapView;
	private readonly Control _combatView;
	private readonly HeaderBar _headerBar;
	private readonly HeaderBar _immersiveHeaderBar;
	private readonly MessageLog _messageLog;
	private readonly MessageLog _immersiveMessageLog;
	private int _explorationVariantIndex;
	private string _currentSceneKey = ExplorationSceneKeys.OutpostEntrance;
	private CompassDirection _facing = CompassDirection.North;
	private IReadOnlyList<string>? _overlayPrompts;
	private string? _selectedRegionalLocationId;

	public ShellPresentationController(
		ExplorationView explorationView,
		RegionalMapView regionalMapView,
		Control combatView,
		HeaderBar headerBar,
		HeaderBar immersiveHeaderBar,
		MessageLog messageLog,
		MessageLog immersiveMessageLog)
	{
		_explorationView = explorationView;
		_regionalMapView = regionalMapView;
		_combatView = combatView;
		_headerBar = headerBar;
		_immersiveHeaderBar = immersiveHeaderBar;
		_messageLog = messageLog;
		_immersiveMessageLog = immersiveMessageLog;

		_regionalMapView.LocationFocused += OnRegionalLocationFocused;
		_regionalMapView.LocationActivated += OnRegionalLocationActivated;
	}

	public PresentationMode CurrentMode { get; private set; }

	public void ShowExploration()
	{
		ShowExplorationAt(MockRegionalMapContent.Outpost);
	}

	// M7e: the deterministic regional-map -> exploration transition. Falls
	// back to the outpost for an unknown ID rather than throwing — this is
	// presentation, not validation; a real backend would be the one place
	// entitled to reject an actual illegal destination.
	public void EnterRegionalLocation(string locationId)
	{
		ShowExplorationAt(
			MockRegionalMapContent.Find(locationId) ?? MockRegionalMapContent.Outpost);
	}

	private void ShowExplorationAt(RegionalLocationDefinition location)
	{
		CurrentMode = PresentationMode.Exploration;

		_explorationView.Show();
		_regionalMapView.Hide();
		_combatView.Hide();

		_currentSceneKey = location.ExplorationSceneKey;
		_facing = CompassDirection.North;
		_overlayPrompts = null;
		RefreshExplorationView();

		SetHeader(location.Label, "Exploration");
		SetMessage($"You stand at {location.Label}.");
	}

	public string? SelectedRegionalLocationId => _selectedRegionalLocationId;

	public void ShowRegionalMap()
	{
		CurrentMode = PresentationMode.RegionalMap;

		_explorationView.Hide();
		_regionalMapView.Show();
		_combatView.Hide();

		_selectedRegionalLocationId = null;
		_regionalMapView.Configure(MockRegionalMapContent.BuildViewModel(null));

		SetHeader("Frontier Region", "Regional Travel");
		SetMessage("The surrounding region stretches before the party.");
	}

	// M7a: SelectionList (the view's own component) owns cursor movement;
	// this just keeps the map's pins/route-preview and this controller's
	// notion of "what's current" in sync with it, the same reactive-view
	// pattern ExplorationView already uses for facing (M6f) — the view
	// never tracks its own state, it is only ever told what to show.
	private void OnRegionalLocationFocused(string locationId)
	{
		_selectedRegionalLocationId = locationId;
		_regionalMapView.SetSelectedLocation(
			locationId,
			MockRegionalMapContent.BuildViewModel(locationId).RoutePreview);
	}

	public void CycleRegionalMapZoom(bool zoomIn)
	{
		_regionalMapView.CycleZoom(zoomIn);
	}

	private void OnRegionalLocationActivated(string locationId)
	{
		OnRegionalLocationFocused(locationId);

		RegionalLocationDefinition? location = MockRegionalMapContent.Find(locationId);
		SetMessage($"{location?.Label ?? locationId} selected as destination.");
	}

	public void ShowCombat()
	{
		CurrentMode = PresentationMode.Combat;

		_explorationView.Hide();
		_regionalMapView.Hide();
		_combatView.Show();

		SetHeader("Encounter", "Combat");
		SetMessage("Combat has begun.");
	}

	public string CycleExplorationVariant()
	{
		_explorationVariantIndex =
			(_explorationVariantIndex + 1) % ExplorationVariantSceneKeys.Length;
		_currentSceneKey = ExplorationVariantSceneKeys[_explorationVariantIndex];

		RefreshExplorationView();

		return _currentSceneKey;
	}

	// M6f: the first real player action (a keypress, not mock data — see
	// M6d) to change what ExplorationView visibly shows rather than just
	// printing a message.
	public void TurnFacing(bool turnLeft)
	{
		_facing = turnLeft
			? CompassDirectionPresentation.TurnLeft(_facing)
			: CompassDirectionPresentation.TurnRight(_facing);

		RefreshExplorationView();
	}

	// M6f: local-status presentation as a capability — CommandBar's own
	// ShowMovementPrompt already communicates the exploration-movement
	// hint below the viewport, so wiring this to movement mode too would
	// have shown the same instruction twice on screen at once. No placed
	// objects/NPCs exist yet to drive it with real content (M6e's own
	// finding, still true), so this is proven correct without a
	// production caller — see the milestone doc's M6g entry for how.
	public void SetOverlayPrompts(IReadOnlyList<string>? prompts)
	{
		_overlayPrompts = prompts;

		RefreshExplorationView();
	}

	private void RefreshExplorationView()
	{
		_explorationView.Configure(new ExplorationViewModel(
			_currentSceneKey,
			CompassDirectionPresentation.ToFacingText(_facing),
			CompassDirectionPresentation.ToCompassLetter(_facing),
			_overlayPrompts));
	}

	public void SetMessage(string message)
	{
		_messageLog.SetMessage(message);
		_immersiveMessageLog.SetMessage(message);
	}

	private void SetHeader(string location, string mode)
	{
		_headerBar.SetLocationAndMode(location, mode);
		_immersiveHeaderBar.SetLocationAndMode(location, mode);
	}
}
