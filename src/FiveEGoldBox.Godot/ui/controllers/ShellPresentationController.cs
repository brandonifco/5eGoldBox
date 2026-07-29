using System.Collections.Generic;
using Godot;

// Split into partials by concern (M8d) — the same 250-line pattern
// ShellInteractionController and RegionalMapView already adopted. This
// file keeps exploration presentation and the members shared across all
// three modes (SetMessage/SetHeader, the constructor). Regional-map
// state is in ShellPresentationController.RegionalMap.cs, combat state
// is in ShellPresentationController.Combat.cs.
internal sealed partial class ShellPresentationController : IShellPresentation
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
	private readonly CombatView _combatView;
	private readonly HeaderBar _headerBar;
	private readonly HeaderBar _immersiveHeaderBar;
	private readonly MessageLog _messageLog;
	private readonly MessageLog _immersiveMessageLog;
	private int _explorationVariantIndex;
	private string _currentSceneKey = ExplorationSceneKeys.OutpostEntrance;
	private CompassDirection _facing = CompassDirection.North;
	private IReadOnlyList<string>? _overlayPrompts;

	public ShellPresentationController(
		ExplorationView explorationView,
		RegionalMapView regionalMapView,
		CombatView combatView,
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
		_combatView.CombatantActivated += id => CombatantTargeted?.Invoke(id);
		_combatView.CellActivated += (x, y) => CombatCellTargeted?.Invoke(x, y);
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

	// Real-session variant — a raw sceneKey/location/mode/message rather
	// than a RegionalLocationDefinition, since a real session's locations
	// don't come from MockRegionalMapContent's calibrated frontier
	// content. Still resets facing/overlay prompts the same way, so a
	// real session gets the same facing/compass badges a mock one does.
	public void ShowExploration(
		string sceneKey,
		string location,
		string mode,
		string message)
	{
		CurrentMode = PresentationMode.Exploration;

		_explorationView.Show();
		_regionalMapView.Hide();
		_combatView.Hide();

		_currentSceneKey = sceneKey;
		_facing = CompassDirection.North;
		_overlayPrompts = null;
		RefreshExplorationView();

		SetHeader(location, mode);
		SetMessage(message);
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

	public void SetHeader(string location, string mode)
	{
		_headerBar.SetLocationAndMode(location, mode);
		_immersiveHeaderBar.SetLocationAndMode(location, mode);
	}
}
