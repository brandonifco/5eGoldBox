using Godot;

public partial class PartyMemberRow : PanelContainer
{
	// White when the party cursor (PageUp/PageDown) highlights this row,
	// the theme's own gold otherwise -- an explicit color override rather
	// than swapping ThemeTypeVariation, since neither existing variation
	// is literal white (ShellBodyLabel is a near-white off-tone) and the
	// user asked for white specifically.
	private static readonly Color HighlightedColor = Colors.White;
	private static readonly Color NormalColor =
		new(0.9254902f, 0.78039217f, 0.36078432f);

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
		_memberNameLabel.AddThemeColorOverride(
			"font_color",
			Selected ? HighlightedColor : NormalColor);
	}
}
