internal sealed class ShellPartyPreviewController : IShellPartyPreview
{
	private static readonly (string Name, string Health)[] PreviewParty =
	{
		("Aric", "12 / 12"),
		("Brenna", "10 / 10"),
		("Cael", "9 / 9"),
		("Daria", "11 / 11"),
		("Edrin", "8 / 8"),
		("Faelan", "7 / 7"),
	};

	private readonly PartySidebar _partySidebar;
	private readonly PartySidebar _immersivePartySidebar;

	public ShellPartyPreviewController(
		PartySidebar partySidebar,
		PartySidebar immersivePartySidebar)
	{
		_partySidebar = partySidebar;
		_immersivePartySidebar = immersivePartySidebar;

		PopulateStandardPreview();
	}

	public void RefreshImmersivePreview()
	{
		_immersivePartySidebar.ClearMembers();

		foreach (PartyMemberRow partyMember in _partySidebar.GetMembers())
		{
			_immersivePartySidebar.AddMember(
				partyMember.MemberName,
				partyMember.HealthText,
				partyMember.Selected);
		}
	}

	// The real party, replacing the hardcoded preview -- called every real
	// render (ShowRealSession/ShowRealCombat) so HP stays live, the same
	// way the command bar itself is rebuilt on every refresh rather than
	// diffed. member.Selected marks whichever member the party cursor
	// (PageUp/PageDown) currently has highlighted.
	public void ShowParty(PartyViewModel party)
	{
		_partySidebar.ClearMembers();

		foreach (PartyMemberViewModel member in party.Members)
		{
			_partySidebar.AddMember(
				member.DisplayName,
				member.HealthText,
				member.Selected);
		}

		RefreshImmersivePreview();
	}

	// Reverts to the hardcoded demo roster -- called by the mock top-level
	// entry points (ShowExploration/ShowRegionalMap/ShowCombat) the same
	// way they already reset _activeRealSession, so leaving a real session
	// doesn't leave its party stuck in the sidebar.
	public void ShowMockPreview()
	{
		PopulateStandardPreview();
		RefreshImmersivePreview();
	}

	private void PopulateStandardPreview()
	{
		_partySidebar.ClearMembers();

		foreach ((string name, string health) in PreviewParty)
		{
			_partySidebar.AddMember(name, health);
		}
	}
}
