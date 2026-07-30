using System;
using System.Collections.Generic;

internal interface IShellPresentation
{
	void ShowExploration();

	// Real-session variant: sets the exploration scene, header, and message
	// in one call rather than relying on ShowExploration()'s hardcoded
	// outpost defaults.
	void ShowExploration(
		string sceneKey,
		string location,
		string mode,
		string message);

	void ShowRegionalMap();

	// Real-session variant: renders actual journey progress (origin,
	// destination, how far along) rather than ShowRegionalMap()'s generic
	// placeholder text. Call after ShowRegionalMap().
	void ConfigureRegionalMap(RegionalMapViewModel model);

	void ShowCombat();

	// Real-session variant: renders a real encounter's actual battlefield
	// (grid dimensions, combatants, HP, whose turn it is) rather than
	// ShowCombat()'s mock content. Call instead of ShowCombat().
	void ConfigureCombat(CombatViewModel model);

	// A real top-down grid of the current exploration floor — walkable/
	// blocked cells, stairs, and the party's real position/facing.
	// EnterAreaMapMode calls this to open it and again after every real
	// move/turn while it's shown, so the marker tracks the party live;
	// ShowExploration(...) (via ExitExplorationMovementMode) closes it.
	void ShowAreaMap(AreaMapViewModel model);

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

	void SetHeader(string location, string mode);
}
