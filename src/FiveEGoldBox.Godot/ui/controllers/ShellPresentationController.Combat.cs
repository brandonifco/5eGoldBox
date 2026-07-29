using System;
using System.Collections.Generic;
using System.Linq;

// Combat presentation state — split out of ShellPresentationController.cs
// (see that file's header). M8d/e.
internal sealed partial class ShellPresentationController
{
	private IReadOnlyList<CombatantMarkerViewModel> _combatCombatants =
		MockCombatContent.Combatants;
	private string? _activeCombatantId;
	private string? _combatSelectedTargetId;
	private IReadOnlyList<CombatHighlightViewModel>? _combatHighlights;

	public event Action<string>? CombatantTargeted;
	public event Action<int, int>? CombatCellTargeted;

	public CombatantMarkerViewModel? ActiveCombatant =>
		_combatCombatants.FirstOrDefault(combatant => combatant.Id == _activeCombatantId);

	public void ShowCombat()
	{
		CurrentMode = PresentationMode.Combat;

		_explorationView.Hide();
		_regionalMapView.Hide();
		_combatView.Show();

		_combatCombatants = MockCombatContent.Combatants;
		_activeCombatantId = MockCombatContent.AllyIdsInOrder.First();
		_combatSelectedTargetId = null;
		_combatHighlights = null;
		RefreshCombatView();

		SetHeader("Frontier Outpost", "Combat");
		SetMessage("Raiders block the outpost gate!");
	}

	public void ShowCombatHighlights(IReadOnlyList<CombatHighlightViewModel>? highlights)
	{
		_combatHighlights = highlights;
		RefreshCombatView();
	}

	public void SelectCombatTarget(string? combatantId)
	{
		_combatSelectedTargetId = combatantId;
		RefreshCombatView();
	}

	// M8e: presentation-only reposition — no path, no legality, just
	// "the marker is here now." Matches this milestone's own scope
	// ("without implementing combat rules").
	public void MoveActiveCombatant(int gridX, int gridY)
	{
		if (_activeCombatantId is null)
		{
			return;
		}

		_combatCombatants = _combatCombatants
			.Select(combatant => combatant.Id == _activeCombatantId
				? combatant with { GridX = gridX, GridY = gridY }
				: combatant)
			.ToList();

		RefreshCombatView();
	}

	// M8e: round-robin among allies only — enemy turns are not simulated,
	// there is no turn engine here, only whose marker the shell treats as
	// active next.
	public void AdvanceCombatTurn()
	{
		List<string> allies = MockCombatContent.AllyIdsInOrder.ToList();
		int currentIndex = allies.IndexOf(_activeCombatantId ?? string.Empty);

		_activeCombatantId = allies[(currentIndex + 1) % allies.Count];
		_combatSelectedTargetId = null;
		_combatHighlights = null;
		RefreshCombatView();
	}

	private void RefreshCombatView()
	{
		IReadOnlyList<CombatantMarkerViewModel> combatants = _combatCombatants
			.Select(combatant => combatant with
			{
				Active = combatant.Id == _activeCombatantId,
				Selected = combatant.Id == _combatSelectedTargetId,
			})
			.ToList();

		_combatView.Configure(new CombatViewModel(
			MockCombatContent.GridWidth,
			MockCombatContent.GridHeight,
			combatants,
			_activeCombatantId,
			_combatSelectedTargetId,
			_combatHighlights));
	}
}
