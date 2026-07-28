public partial class AppShell
{
	private void ShowExplorationView()
	{
		_interactionController.ShowExploration();
	}

	private void ShowRegionalMapView()
	{
		_interactionController.ShowRegionalMap();
	}

	private void ShowCombatView()
	{
		_interactionController.ShowCombat();
	}

	private void ReportMovement(UiCommandIntent intent)
	{
		_interactionController.ReportMovement(intent);
	}
}
