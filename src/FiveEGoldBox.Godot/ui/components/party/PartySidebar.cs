using System.Collections.Generic;
using Godot;

public partial class PartySidebar : PanelContainer
{
	[Export]
	public string TitleText { get; set; } = "Party";

	[Export]
	public bool Immersive { get; set; }

	[Export]
	public PackedScene PartyMemberRowScene { get; set; } = null!;

	private Label _partyTitle = null!;
	private VBoxContainer _partyMembers = null!;

	public override void _Ready()
	{
		_partyTitle = GetNode<Label>("%PartyTitle");
		_partyMembers = GetNode<VBoxContainer>("%PartyMembers");

		ApplyThemeVariation();
		_partyTitle.Text = TitleText;
	}

	public IEnumerable<PartyMemberRow> GetMembers()
	{
		foreach (Node child in _partyMembers.GetChildren())
		{
			if (child is PartyMemberRow partyMember)
			{
				yield return partyMember;
			}
		}
	}

	public void ClearMembers()
	{
		foreach (Node child in _partyMembers.GetChildren())
		{
			_partyMembers.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void AddMember(
		string memberName,
		string healthText,
		bool selected = false)
	{
		PartyMemberRow row =
			PartyMemberRowScene.Instantiate<PartyMemberRow>();

		row.Configure(memberName, healthText, selected);
		_partyMembers.AddChild(row);
	}

	private void ApplyThemeVariation()
	{
		if (Immersive)
		{
			ThemeTypeVariation = "ImmersiveOverlayPanel";
		}
	}
}
