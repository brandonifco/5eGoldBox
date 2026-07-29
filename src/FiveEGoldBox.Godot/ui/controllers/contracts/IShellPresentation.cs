using System.Collections.Generic;

internal interface IShellPresentation
{
	void ShowExploration();

	void ShowRegionalMap();

	void ShowCombat();

	void TurnFacing(bool turnLeft);

	void SetOverlayPrompts(IReadOnlyList<string>? prompts);

	void SetMessage(string message);
}
