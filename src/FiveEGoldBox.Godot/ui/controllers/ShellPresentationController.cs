using System;
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
	private readonly AreaMapView _areaMapView;
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
		AreaMapView areaMapView,
		HeaderBar headerBar,
		HeaderBar immersiveHeaderBar,
		MessageLog messageLog,
		MessageLog immersiveMessageLog)
	{
		_explorationView = explorationView;
		_regionalMapView = regionalMapView;
		_combatView = combatView;
		_areaMapView = areaMapView;
		_headerBar = headerBar;
		_immersiveHeaderBar = immersiveHeaderBar;
		_messageLog = messageLog;
		_immersiveMessageLog = immersiveMessageLog;

		_regionalMapView.LocationFocused += OnRegionalLocationFocused;
		_regionalMapView.LocationActivated += OnRegionalLocationActivated;
		_combatView.CombatantActivated += id => CombatantTargeted?.Invoke(id);
		_combatView.CellActivated += (x, y) => CombatCellTargeted?.Invoke(x, y);
		_combatView.CellCursorFocused += (x, y) => CombatCellCursorFocused?.Invoke(x, y);
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
		_areaMapView.Hide();

		_currentSceneKey = location.ExplorationSceneKey;
		_facing = CompassDirection.North;
		_overlayPrompts = null;
		RefreshExplorationView();
		ConfigureExplorationCorridor(null);

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
		_areaMapView.Hide();

		_currentSceneKey = sceneKey;
		_facing = CompassDirection.North;
		_overlayPrompts = null;
		RefreshExplorationView();

		SetHeader(location, mode);
		SetMessage(message);
	}

	// The real integration seam's own layer -- mock content always
	// passes null (clearing back to plain background), since none of it
	// has a real floor to render; a real session opts in explicitly with
	// real data right after showing exploration. See
	// ExplorationView.ConfigureCorridor for what this actually renders.
	//
	// Also syncs the Facing/compass badge to the session's real facing.
	// ShowExploration(...) always resets _facing to North regardless of
	// a real session's actual starting facing (a debug-jump can start
	// facing anything), and TurnFacing only ever rotates relative to
	// that possibly-wrong starting point -- so a debug-jumped session's
	// badge silently drifts out of sync with the real backend facing by
	// a fixed offset for the rest of the session, even though movement
	// itself (and this corridor art) is reading real facing correctly
	// the whole time. Found while verifying this exact corridor feature
	// against a debug jump; pre-existing, not introduced by it.
	public void ConfigureExplorationCorridor(AreaMapViewModel? map)
	{
		_explorationView.ConfigureCorridor(map);

		if (map is not null
			&& Enum.TryParse(map.PartyFacing, out CompassDirection realFacing))
		{
			_facing = realFacing;
			RefreshExplorationView();
		}
	}

	public string CycleExplorationVariant()
	{
		_explorationVariantIndex =
			(_explorationVariantIndex + 1) % ExplorationVariantSceneKeys.Length;
		_currentSceneKey = ExplorationVariantSceneKeys[_explorationVariantIndex];

		RefreshExplorationView();
		ConfigureExplorationCorridor(null);

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
