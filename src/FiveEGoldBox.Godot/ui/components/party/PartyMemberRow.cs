using Godot;

public partial class PartyMemberRow : PanelContainer
{
	[Export]
	public string MemberName { get; set; } = string.Empty;

	[Export]
	public string HealthText { get; set; } = string.Empty;

	[Export]
	public bool Selected { get; set; }

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
		string healthText,
		bool selected = false)
	{
		MemberName = memberName;
		HealthText = healthText;
		Selected = selected;

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	private void ApplyContent()
	{
		_memberNameLabel.Text = MemberName;
		_healthLabel.Text = HealthText;
		// Reuses the existing gold "emphasis" text style rather than a new
		// StyleBox -- the party cursor (PageUp/PageDown) marks whichever
		// member View/Cast/Inventory currently act on.
		_memberNameLabel.ThemeTypeVariation = Selected
			? "ShellEmphasisLabel"
			: "ShellBodyLabel";
	}
}
