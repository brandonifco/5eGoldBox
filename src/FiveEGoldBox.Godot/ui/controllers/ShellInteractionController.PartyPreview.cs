using System.Collections.Generic;
using System.Linq;

// The party cursor (PageUp/PageDown) -- matches the classic Gold Box games'
// own party-panel navigation: whichever member is highlighted here is who
// View/Character acts on, not a second list to click through inside that
// screen. Mock sessions don't get a cursor (there's nothing real to
// highlight); ShowMockPreview already handles that path on its own.
internal sealed partial class ShellInteractionController
{
	private string? _highlightedPartyMemberId;

	public void CyclePartyHighlight(int direction)
	{
		var ids = GetOrderedPartyMemberIds();

		if (ids.Count == 0)
		{
			return;
		}

		int currentIndex = _highlightedPartyMemberId is null
			? -1
			: ids.IndexOf(_highlightedPartyMemberId);
		int nextIndex = ((currentIndex + direction) % ids.Count
			+ ids.Count) % ids.Count;

		_highlightedPartyMemberId = ids[nextIndex];
		RefreshPartyPreview();
	}

	// Reachable from both ShowRealSession (exploration/outpost/travel) and
	// ShowRealCombat -- combat's own live per-turn HP lives on the
	// battlefield's CombatantMarkerViewModels, not on RealGameSession
	// (which only catches up once the fight ends), so this reads whichever
	// source is actually live right now rather than always preferring one.
	private void RefreshPartyPreview()
	{
		if (_activeCombatSession is not null)
		{
			ShowPartyPreviewFromCombat(_activeCombatSession.Describe());
			return;
		}

		if (_activeRealSession is not null)
		{
			_partyPreview.ShowParty(
				ApplyPartyHighlight(_activeRealSession.DescribePartyPreview()));
		}
	}

	private List<string> GetOrderedPartyMemberIds()
	{
		if (_activeCombatSession is not null)
		{
			return _activeCombatSession.Describe().View.Combatants
				.Where(combatant => combatant.IsAlly)
				.Select(combatant => combatant.Id)
				.ToList();
		}

		if (_activeRealSession is not null)
		{
			return _activeRealSession.DescribePartyPreview().Members
				.Select(member => member.Id)
				.ToList();
		}

		return new List<string>();
	}

	// Whoever the party cursor currently marks, defaulting to the first
	// party member the first time this is asked (before any PageUp/
	// PageDown press) -- mirrors DescribeCharacter's own old "first member"
	// default, just no longer hardcoded to it once a highlight is set.
	private string GetHighlightedOrFirstPartyMemberId()
	{
		if (_highlightedPartyMemberId is not null)
		{
			return _highlightedPartyMemberId;
		}

		var ids = GetOrderedPartyMemberIds();
		return ids.Count > 0 ? ids[0] : string.Empty;
	}

	private PartyViewModel ApplyPartyHighlight(PartyViewModel party)
	{
		string highlightedId = _highlightedPartyMemberId
			?? (party.Members.Count > 0 ? party.Members[0].Id : string.Empty);

		return new PartyViewModel(party.Members
			.Select(member => member with
			{
				Selected = member.Id == highlightedId
			})
			.ToArray());
	}
}
