using System;
using System.Collections.Generic;

internal interface IShellPresentation
{
	void ShowExploration();

	void ShowRegionalMap();

	void ShowCombat();

	string? SelectedRegionalLocationId { get; }

	void EnterRegionalLocation(string locationId);

	void TurnFacing(bool turnLeft);

	void SetOverlayPrompts(IReadOnlyList<string>? prompts);

	void CycleRegionalMapZoom(bool zoomIn);

	// M8e: fires when the player targets a combatant or a highlighted
	// grid cell — ShellInteractionController owns what either one means
	// for whichever command is currently pending.
	event Action<string>? CombatantTargeted;
	event Action<int, int>? CombatCellTargeted;

	CombatantMarkerViewModel? ActiveCombatant { get; }

	void ShowCombatHighlights(IReadOnlyList<CombatHighlightViewModel>? highlights);

	void SelectCombatTarget(string? combatantId);

	void MoveActiveCombatant(int gridX, int gridY);

	void AdvanceCombatTurn();

	void SetMessage(string message);
}
