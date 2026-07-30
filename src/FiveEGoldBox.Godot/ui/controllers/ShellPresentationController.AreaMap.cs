// Area-map presentation state — split out of ShellPresentationController.cs
// (see that file's header) the same way RegionalMap/Combat state already
// were. Look-only: no highlights, no target selection, nothing beyond
// showing the current floor's real grid and the party's real
// position/facing.
internal sealed partial class ShellPresentationController
{
	public void ShowAreaMap(AreaMapViewModel model)
	{
		CurrentMode = PresentationMode.AreaMap;

		_explorationView.Hide();
		_regionalMapView.Hide();
		_combatView.Hide();
		_areaMapView.Show();

		_areaMapView.Configure(model);
	}
}
