public partial class AppShell
{
	private void ExitExplorationMovementMode()
	{
		_interactionController.ExitExplorationMovementMode();
	}

	private void RefreshCommandBars()
	{
		_commandBarController.Refresh();
	}
}
