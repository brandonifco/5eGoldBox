using Godot;

public partial class HeaderBar : PanelContainer
{
	[Export]
	public string HeaderText { get; set; } =
		"Location: Outpost | Mode: Exploration";

	[Export]
	public bool Immersive { get; set; }

	private Label _headerLabel = null!;

	public override void _Ready()
	{
		_headerLabel = GetNode<Label>("%HeaderLabel");
		ApplyThemeVariation();
		ApplyContent();
	}

	public void SetLocationAndMode(
		string location,
		string mode)
	{
		HeaderText = $"Location: {location} | Mode: {mode}";

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	private void ApplyThemeVariation()
	{
		if (Immersive)
		{
			ThemeTypeVariation = "ImmersiveOverlayPanel";
		}
	}

	private void ApplyContent()
	{
		_headerLabel.Text = HeaderText;
	}
}
