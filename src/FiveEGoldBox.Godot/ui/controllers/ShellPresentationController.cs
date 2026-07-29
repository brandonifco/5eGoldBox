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
	private readonly Control _regionalMapView;
	private readonly Control _combatView;
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
		Control regionalMapView,
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
	}

	public PresentationMode CurrentMode { get; private set; }

	public void ShowExploration()
	{
		CurrentMode = PresentationMode.Exploration;

		_explorationView.Show();
		_regionalMapView.Hide();
		_combatView.Hide();

		_currentSceneKey = ExplorationSceneKeys.OutpostEntrance;
		_facing = CompassDirection.North;
		_overlayPrompts = null;
		RefreshExplorationView();

		SetHeader("Outpost", "Exploration");
		SetMessage("You stand at the entrance to the outpost.");
	}

	public void ShowRegionalMap()
	{
		CurrentMode = PresentationMode.RegionalMap;

		_explorationView.Hide();
		_regionalMapView.Show();
		_combatView.Hide();

		SetHeader("Wilderness", "Regional Travel");
		SetMessage("The surrounding region stretches before the party.");
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
