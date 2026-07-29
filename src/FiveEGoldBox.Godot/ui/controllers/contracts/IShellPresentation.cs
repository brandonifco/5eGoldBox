internal interface IShellPresentation
{
	void ShowExploration();

	void ShowRegionalMap();

	void ShowCombat();

	void TurnFacing(bool turnLeft);

	void SetExplorationMovementOverlayActive(bool active);

	void SetMessage(string message);
}
