using System.Collections.Generic;
using System.Linq;

// The Outpost Gate skirmish's real driving content — analogous to
// MockRegionalMapContent's role for the regional map (M7a): not the QA/
// stress-test catalog (that's MockCombatScenarios, M5c, "one method per
// minimum case"), this is the actual pre-integration content the live
// CombatView renders. Grid positions are calibrated by eye against
// images/tactical_combat_view_01.png's own baked-in character
// illustrations and colored ring markers, the same way M7a's location
// positions were calibrated against the regional map art — flagged for a
// human look for the same reason.
internal static class MockCombatContent
{
	public const int GridWidth = 20;
	public const int GridHeight = 16;

	// ActiveCombatantId/SelectedCombatantId are the source of truth, not
	// each CombatantMarkerViewModel's own Active/Selected fields (which
	// Combatants below always leaves at their record defaults) — derived
	// here the same way MockRegionalMapContent.BuildViewModel derives
	// each location marker's Selected flag from SelectedLocationId (M7a),
	// rather than requiring two places to agree by hand.
	public static CombatViewModel BuildViewModel(
		string? activeCombatantId = "aric",
		string? selectedCombatantId = null,
		IReadOnlyList<CombatHighlightViewModel>? highlights = null,
		IReadOnlyList<string>? informationalOverlays = null)
	{
		IReadOnlyList<CombatantMarkerViewModel> combatants = Combatants
			.Select(combatant => combatant with
			{
				Active = combatant.Id == activeCombatantId,
				Selected = combatant.Id == selectedCombatantId,
			})
			.ToList();

		return new CombatViewModel(
			GridWidth,
			GridHeight,
			combatants,
			activeCombatantId,
			selectedCombatantId,
			highlights,
			informationalOverlays);
	}

	// Six named party members, matching ShellPartyPreviewController's own
	// preview roster — one identity, not a second invented one, across
	// the party sidebar and the combat grid.
	public static readonly IReadOnlyList<CombatantMarkerViewModel> Combatants =
		new[]
		{
			new CombatantMarkerViewModel("aric", "Aric", 9, 9),
			new CombatantMarkerViewModel("brenna", "Brenna", 11, 9),
			new CombatantMarkerViewModel("cael", "Cael", 7, 11),
			new CombatantMarkerViewModel("daria", "Daria", 12, 11),
			new CombatantMarkerViewModel("edrin", "Edrin", 9, 13),
			new CombatantMarkerViewModel("faelan", "Faelan", 11, 13),
			new CombatantMarkerViewModel(
				"raider-1", "Raider", 10, 5, IsAlly: false),
			new CombatantMarkerViewModel(
				"raider-2", "Raider", 11, 6, IsAlly: false),
		};

	public static IEnumerable<string> AllyIdsInOrder =>
		Combatants.Where(combatant => combatant.IsAlly).Select(combatant => combatant.Id);

	public static CombatantMarkerViewModel? Find(string combatantId) =>
		Combatants.FirstOrDefault(combatant => combatant.Id == combatantId);

	// M8e: presentation-only move range — the four orthogonal neighbors
	// of the active combatant, clamped to the grid. Not a real movement
	// rule (no terrain, no occupied-cell check, no speed) — this milestone
	// implements combat *presentation*, not combat rules.
	public static IReadOnlyList<CombatHighlightViewModel> MoveRangeHighlights(
		CombatantMarkerViewModel active)
	{
		(int dx, int dy)[] offsets = { (0, -1), (0, 1), (-1, 0), (1, 0) };

		return offsets
			.Select(offset => (X: active.GridX + offset.dx, Y: active.GridY + offset.dy))
			.Where(cell => cell.X >= 0 && cell.X < GridWidth &&
				cell.Y >= 0 && cell.Y < GridHeight)
			.Select(cell => new CombatHighlightViewModel(cell.X, cell.Y, "move-range"))
			.ToList();
	}

	// M8e: every combatant on the other side of the given one is a
	// "valid target" here — again presentation only, no line-of-sight or
	// range rule behind it.
	public static IReadOnlyList<CombatHighlightViewModel> TargetHighlights(bool targetAllies)
	{
		return Combatants
			.Where(combatant => combatant.IsAlly == targetAllies)
			.Select(combatant => new CombatHighlightViewModel(
				combatant.GridX, combatant.GridY, "valid-target"))
			.ToList();
	}
}
