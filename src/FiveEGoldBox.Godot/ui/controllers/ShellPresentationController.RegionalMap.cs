// Regional-map presentation state — split out of
// ShellPresentationController.cs (see that file's header).
internal sealed partial class ShellPresentationController
{
	private string? _selectedRegionalLocationId;

	// True between ConfigureRegionalMap (a real session's journey
	// progress) and the next ShowRegionalMap (mock Frontier Region
	// content) — guards OnRegionalLocationFocused/Activated from
	// rebuilding a real session's map with mock content just because the
	// player focused/clicked one of its two markers.
	private bool _regionalMapIsRealSession;

	public string? SelectedRegionalLocationId => _selectedRegionalLocationId;

	public void ShowRegionalMap()
	{
		CurrentMode = PresentationMode.RegionalMap;

		_explorationView.Hide();
		_regionalMapView.Show();
		_combatView.Hide();
		_areaMapView.Hide();

		_selectedRegionalLocationId = null;
		_regionalMapIsRealSession = false;
		_regionalMapView.Configure(MockRegionalMapContent.BuildViewModel(null));

		SetHeader("Frontier Region", "Regional Travel");
		SetMessage("The surrounding region stretches before the party.");
	}

	// Real-session variant: real journey progress (RealGameSession.
	// DescribeRegionalMap) rendered through the same RegionalMapView the
	// mock Frontier Region content uses — RegionalMapView itself switches
	// off the calibrated map art when it sees this model's real MapKey
	// (see RegionalMapView.Markers.cs). Call after ShowRegionalMap().
	public void ConfigureRegionalMap(RegionalMapViewModel model)
	{
		_regionalMapIsRealSession = true;
		_regionalMapView.Configure(model);
	}

	// M7a: SelectionList (the view's own component) owns cursor movement;
	// this just keeps the map's pins/route-preview and this controller's
	// notion of "what's current" in sync with it, the same reactive-view
	// pattern ExplorationView already uses for facing (M6f) — the view
	// never tracks its own state, it is only ever told what to show.
	// A real session's own two-point track has nothing for this to
	// rebuild towards (there is no "travel to" action reachable by
	// clicking the map), so it only tracks which marker is focused there.
	private void OnRegionalLocationFocused(string locationId)
	{
		_selectedRegionalLocationId = locationId;

		if (_regionalMapIsRealSession)
		{
			return;
		}

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

		if (_regionalMapIsRealSession)
		{
			return;
		}

		RegionalLocationDefinition? location = MockRegionalMapContent.Find(locationId);
		SetMessage($"{location?.Label ?? locationId} selected as destination.");
	}
}
