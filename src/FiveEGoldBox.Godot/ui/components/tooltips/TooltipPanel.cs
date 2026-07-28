using Godot;

public partial class TooltipPanel : PanelContainer
{
	[Export]
	public string TitleText { get; set; } = "Tooltip";

	[Export]
	public string BodyText { get; set; } = string.Empty;

	private Label _titleLabel = null!;
	private Label _bodyLabel = null!;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_bodyLabel = GetNode<Label>("%BodyLabel");
		ApplyContent();
	}

	public void SetContent(
		string title,
		string body)
	{
		TitleText = title;
		BodyText = body;

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	public void ShowTooltip(
		string title,
		string body)
	{
		SetContent(title, body);
		Show();
	}

	public void HideTooltip()
	{
		Hide();
	}

	private void ApplyContent()
	{
		_titleLabel.Text = TitleText;
		_titleLabel.Visible = !string.IsNullOrWhiteSpace(TitleText);
		_bodyLabel.Text = BodyText;
	}
}
