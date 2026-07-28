using Godot;

public partial class PartyMemberRow : PanelContainer
{
	[Export]
	public string MemberName { get; set; } = string.Empty;

	[Export]
	public string HealthText { get; set; } = string.Empty;

	private Label _memberNameLabel = null!;
	private Label _healthLabel = null!;

	public override void _Ready()
	{
		_memberNameLabel = GetNode<Label>("%MemberNameLabel");
		_healthLabel = GetNode<Label>("%HealthLabel");
		ApplyContent();
	}

	public void Configure(
		string memberName,
		string healthText)
	{
		MemberName = memberName;
		HealthText = healthText;

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	private void ApplyContent()
	{
		_memberNameLabel.Text = MemberName;
		_healthLabel.Text = HealthText;
	}
}
