// Regional-map presentation state — split out of
// ShellPresentationController.cs (see that file's header).
internal sealed partial class ShellPresentationController
{
	private string? _selectedRegionalLocationId;

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
}
