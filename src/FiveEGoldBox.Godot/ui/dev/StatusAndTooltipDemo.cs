using Godot;

public partial class StatusAndTooltipDemo : Control
{
	[Export]
	public Theme StandardTheme { get; set; } = null!;

	[Export]
	public Theme HighContrastTheme { get; set; } = null!;

	private TooltipPanel _tooltipPanel = null!;
	private Label _statusLabel = null!;
	private Button _shortTrigger = null!;
	private Button _longTrigger = null!;
	private bool _highContrast;

	public override void _Ready()
	{
		_tooltipPanel = GetNode<TooltipPanel>("%DemoTooltip");
		_statusLabel = GetNode<Label>("%StatusLabel");
		_shortTrigger = GetNode<Button>("%ShortTooltipTrigger");
		_longTrigger = GetNode<Button>("%LongTooltipTrigger");

		BindTooltip(
			_shortTrigger,
			"Watchtower",
			"A fortified lookout protecting the northern road.",
			"short");

		BindTooltip(
			_longTrigger,
			"Long tooltip stress case",
			"This deliberately long description verifies that tooltip body " +
			"text wraps within a constrained width without clipping, " +
			"overlapping neighboring controls, or creating horizontal " +
			"scrolling at the minimum supported presentation size.",
			"long");

		_tooltipPanel.HideTooltip();
		UpdateStatus("Ready — hover or focus either tooltip trigger.");
		_shortTrigger.GrabFocus();
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.CtrlPressed &&
			keyEvent.AltPressed &&
			keyEvent.Keycode == Key.H)
		{
			_highContrast = !_highContrast;
			Theme = _highContrast
				? HighContrastTheme
				: StandardTheme;

			UpdateStatus(
				_highContrast
					? "High-contrast theme enabled."
					: "Standard theme enabled.");

			GetViewport().SetInputAsHandled();
		}
	}

	private void BindTooltip(
		Button trigger,
		string title,
		string body,
		string identifier)
	{
		trigger.MouseEntered += () =>
			ShowTooltip(title, body, identifier, "mouse");
		trigger.FocusEntered += () =>
			ShowTooltip(title, body, identifier, "keyboard focus");
		trigger.MouseExited += () =>
			HideTooltipIfInactive(trigger);
		trigger.FocusExited += () =>
			HideTooltipIfInactive(trigger);
		trigger.Pressed += () =>
			ShowTooltip(title, body, identifier, "activation");
	}

	private void ShowTooltip(
		string title,
		string body,
		string identifier,
		string source)
	{
		_tooltipPanel.ShowTooltip(title, body);
		UpdateStatus($"Showing {identifier} tooltip from {source}.");
	}

	private void HideTooltipIfInactive(Button trigger)
	{
		if (trigger.HasFocus() ||
			trigger.GetGlobalRect().HasPoint(
				GetViewport().GetMousePosition()))
		{
			return;
		}

		_tooltipPanel.HideTooltip();
		UpdateStatus("Tooltip hidden.");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = message;
	}
}
