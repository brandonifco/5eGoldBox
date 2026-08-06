using System.Collections.Generic;
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

	// Accumulated lines for AppendLog's scrolling-transcript mode -- the
	// whole session's adventure log (exploration and combat both), not
	// just combat. Never reset once the app is running: it grows for as
	// long as the client is open, the same way a real DM's own notes
	// would. SetMessage is a separate, one-shot passthrough that never
	// touches this -- it displays a transient prompt (e.g. "Choose a
	// target...") over whatever's currently shown without discarding
	// accumulated log lines, so the next AppendLog restores the full
	// transcript rather than continuing from a message SetMessage
	// clobbered.
	private readonly List<string> _logLines = new();

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

	// Adds to the running transcript.
	public void AppendLog(IReadOnlyList<string> lines)
	{
		_logLines.AddRange(lines);
		RenderLog();
	}

	private void RenderLog()
	{
		MessageText = string.Join("\n", _logLines);

		if (!IsNodeReady())
		{
			return;
		}

		ApplyContent();

		if (!Scrollable)
		{
			return;
		}

		// The RichTextLabel hasn't re-laid-out the new text yet on this same
		// frame, so GetLineCount() would still report the old, shorter
		// count if read synchronously here -- deferred so it reads fresh.
		RichTextLabel label = _messageTextLabel;
		Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(label))
			{
				label.ScrollToLine(label.GetLineCount());
			}
		}).CallDeferred();
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
