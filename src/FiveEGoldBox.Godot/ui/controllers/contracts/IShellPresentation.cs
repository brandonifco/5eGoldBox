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

	// Real-session variant: layers real first-person wall art (doors
	// included) over the front-facing exploration view from the same
	// current-floor snapshot ShowAreaMap renders top-down. Null clears
	// it back to plain background -- see
	// ShellPresentationController.ConfigureExplorationCorridor.
	void ConfigureExplorationCorridor(AreaMapViewModel? map);

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
	event Action<int, int>? CombatCellCursorFocused;

	CombatantMarkerViewModel? ActiveCombatant { get; }

	void ShowCombatHighlights(IReadOnlyList<CombatHighlightViewModel>? highlights);

	void SelectCombatTarget(string? combatantId);

	void MoveActiveCombatant(int gridX, int gridY);

	void AdvanceCombatTurn();

	void SetMessage(string message);

	// The running adventure log -- exploration and combat both, one
	// continuous scrolling transcript for the life of the client. Distinct
	// from SetMessage's single replaced line, which is still used for
	// transient targeting prompts ("Choose a target...") and view-toggle
	// text that layer on top without being added to this.
	void AppendJournal(IReadOnlyList<string> lines);

	void SetHeader(string location, string mode);
}
