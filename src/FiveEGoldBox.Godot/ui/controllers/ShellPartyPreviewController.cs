internal sealed class ShellPartyPreviewController
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
				partyMember.HealthText);
		}
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
