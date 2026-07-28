using Godot;

public partial class MessageLog : PanelContainer
{
	[Export]
	public string TitleText { get; set; } = "Messages";

	[Export]
	public string MessageText { get; set; } = string.Empty;

	[Export]
	public bool Scrollable { get; set; }

	private Label _titleLabel = null!;
	private RichTextLabel _messageTextLabel = null!;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_messageTextLabel = GetNode<RichTextLabel>("%MessageTextLabel");
		ApplyContent();
	}

	public void SetMessage(string message)
	{
		MessageText = message;

		if (IsNodeReady())
		{
			ApplyContent();
		}
	}

	private void ApplyContent()
	{
		_titleLabel.Text = TitleText;
		_messageTextLabel.Text = MessageText;
		_messageTextLabel.FitContent = !Scrollable;
		_messageTextLabel.ScrollActive = Scrollable;
		_messageTextLabel.SizeFlagsVertical = Scrollable
			? Control.SizeFlags.ExpandFill
			: Control.SizeFlags.Fill;
	}
}
