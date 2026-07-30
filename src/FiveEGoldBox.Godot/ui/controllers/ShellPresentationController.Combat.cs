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
	private IReadOnlyList<string>? _combatInformationalOverlays;

	// True between ConfigureCombat (a real encounter) and the next
	// ShowCombat (mock content) — guards MoveActiveCombatant/
	// AdvanceCombatTurn, which reposition/advance turns using
	// MockCombatContent internals that don't apply to real combat, from
	// corrupting real state. SelectCombatTarget/ShowCombatHighlights need
	// no such guard — they only ever touch whichever combatants/grid
	// dimensions are currently stored, real or mock, and stay correct
	// either way. Same shape as ShellPresentationController.RegionalMap.
	// cs's _regionalMapIsRealSession.
	private bool _combatIsRealSession;
	private int _combatGridWidth = MockCombatContent.GridWidth;
	private int _combatGridHeight = MockCombatContent.GridHeight;
	private bool _combatHasArtBackground = true;

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
		_areaMapView.Hide();

		_combatIsRealSession = false;
		_combatGridWidth = MockCombatContent.GridWidth;
		_combatGridHeight = MockCombatContent.GridHeight;
		_combatHasArtBackground = true;
		_combatCombatants = MockCombatContent.Combatants;
		_activeCombatantId = MockCombatContent.AllyIdsInOrder.First();
		_combatSelectedTargetId = null;
		_combatHighlights = null;
		_combatInformationalOverlays = null;
		_combatView.Configure(BuildCombatViewModel());

		SetHeader("Frontier Outpost", "Combat");
		SetMessage("Raiders block the outpost gate!");
	}

	// The real integration seam's own entry point — renders whatever
	// RealCombatSession.Describe() reports, through the exact same
	// CombatView the mock content uses. Grid dimensions and the art-
	// background flag are stored (not just passed straight through to
	// CombatView.Configure) because later lighter updates
	// (SelectCombatTarget/ShowCombatHighlights) call RefreshCombatView,
	// which rebuilds via BuildCombatViewModel — that needs to keep
	// describing a real encounter's actual battlefield, not silently
	// fall back to the mock grid's dimensions.
	public void ConfigureCombat(CombatViewModel model)
	{
		CurrentMode = PresentationMode.Combat;

		_explorationView.Hide();
		_regionalMapView.Hide();
		_combatView.Show();
		_areaMapView.Hide();

		_combatIsRealSession = true;
		_combatGridWidth = model.GridWidth;
		_combatGridHeight = model.GridHeight;
		_combatHasArtBackground = model.HasArtBackground;
		_combatCombatants = model.Combatants;
		_activeCombatantId = model.ActiveCombatantId;
		_combatSelectedTargetId = null;
		_combatHighlights = null;
		_combatInformationalOverlays = null;
		_combatView.Configure(BuildCombatViewModel());
	}

	// M8f: completed-combat presentation — a capability, not yet given a
	// production trigger, since there is no real win/loss condition to
	// reach it from (this milestone implements combat presentation, not
	// combat rules). Verified directly, the same "temporary trigger,
	// screenshot, revert" technique M6e used for ConfirmationDialog.
	public void ShowCompletedEncounter(IReadOnlyList<string> informationalOverlays)
	{
		_combatHighlights = null;
		_combatInformationalOverlays = informationalOverlays;
		RefreshCombatView();
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
		if (_combatIsRealSession || _activeCombatantId is null)
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
		if (_combatIsRealSession)
		{
			return;
		}

		List<string> allies = MockCombatContent.AllyIdsInOrder.ToList();
		int currentIndex = allies.IndexOf(_activeCombatantId ?? string.Empty);

		_activeCombatantId = allies[(currentIndex + 1) % allies.Count];
		_combatSelectedTargetId = null;
		_combatHighlights = null;
		RefreshCombatView();
	}

	private void RefreshCombatView()
	{
		_combatView.Refresh(BuildCombatViewModel());
	}

	private CombatViewModel BuildCombatViewModel()
	{
		IReadOnlyList<CombatantMarkerViewModel> combatants = _combatCombatants
			.Select(combatant => combatant with
			{
				Active = combatant.Id == _activeCombatantId,
				Selected = combatant.Id == _combatSelectedTargetId,
			})
			.ToList();

		return new CombatViewModel(
			_combatGridWidth,
			_combatGridHeight,
			combatants,
			_activeCombatantId,
			_combatSelectedTargetId,
			_combatHighlights,
			_combatInformationalOverlays,
			_combatHasArtBackground);
	}
}
