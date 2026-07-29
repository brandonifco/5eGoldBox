using System;
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

		_explorationView.Configure(
			new ExplorationViewModel(ExplorationSceneKeys.OutpostEntrance));
		SetHeader("Outpost", "Exploration");
		SetMessage("You stand at the entrance to the outpost.");
	}

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

		_explorationView.Configure(new ExplorationViewModel(sceneKey));
		SetHeader(location, mode);
		SetMessage(message);
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

	// Real journey progress rendered through the existing placeholder Label
	// (StandardLayout.tscn's RegionalMapView/RegionalMapCenter/
	// RegionalMapLabel) rather than the spatial marker layout
	// RegionalMapMarkerViewModel/RegionalMapPointViewModel ultimately imply
	// — that needs a real map-rendering component (M7a), which this isn't.
	// "Function before beauty," the same call M6b made for exploration
	// placeholders, applied one milestone early because the real data
	// arrived before the real view did.
	//
	// LocationMarkers[0]/[1] are relied on being origin/destination in that
	// order, matching how RealGameSession.DescribeRegionalMap builds them —
	// not a general contract of RegionalMapViewModel itself.
	public void ConfigureRegionalMap(RegionalMapViewModel model)
	{
		Label label = _regionalMapView.GetNode<Label>(
			"RegionalMapCenter/RegionalMapLabel");

		if (model.LocationMarkers.Count < 2 || model.PartyMarker is null)
		{
			label.Text = "The road stretches ahead.";
			return;
		}

		RegionalMapMarkerViewModel origin = model.LocationMarkers[0];
		RegionalMapMarkerViewModel destination = model.LocationMarkers[1];
		int percent = (int)Math.Round(model.PartyMarker.Position.X);

		label.Text =
			$"{origin.Label}  →  {destination.Label}\n{percent}% of the way there";
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
		string sceneKey = ExplorationVariantSceneKeys[_explorationVariantIndex];

		_explorationView.Configure(new ExplorationViewModel(sceneKey));

		return sceneKey;
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
