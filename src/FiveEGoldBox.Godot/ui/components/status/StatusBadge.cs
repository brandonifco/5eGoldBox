using Godot;

public partial class StatusBadge : PanelContainer
{
	[Export]
	public string StatusText { get; set; } = "Status";

	[Export]
	public StatusBadgeKind Kind { get; set; }

	private Label _statusLabel = null!;

	public override void _Ready()
	{
		_statusLabel = GetNode<Label>("%StatusLabel");
		ApplyStatus();
	}

	public void SetStatus(
		string statusText,
		StatusBadgeKind kind)
	{
		StatusText = statusText;
		Kind = kind;

		if (IsNodeReady())
		{
			ApplyStatus();
		}
	}

	private void ApplyStatus()
	{
		_statusLabel.Text = StatusText;
		_statusLabel.ThemeTypeVariation = Kind switch
		{
			StatusBadgeKind.Healthy => "ShellStatusHealthy",
			StatusBadgeKind.Wounded => "ShellStatusWounded",
			StatusBadgeKind.Critical => "ShellStatusCritical",
			StatusBadgeKind.Incapacitated => "ShellStatusIncapacitated",
			StatusBadgeKind.Removed => "ShellStatusRemoved",
			StatusBadgeKind.PositiveEffect => "ShellStatusPositiveEffect",
			StatusBadgeKind.NegativeEffect => "ShellStatusNegativeEffect",
			_ => "ShellSmallStatus",
		};

		TooltipText = $"{Kind}: {StatusText}";
	}
}
