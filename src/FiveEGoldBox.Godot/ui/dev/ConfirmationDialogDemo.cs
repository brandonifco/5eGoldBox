using Godot;

public partial class ConfirmationDialogDemo : Control
{
	[Export]
	public Theme StandardTheme { get; set; } = null!;

	[Export]
	public Theme HighContrastTheme { get; set; } = null!;

	private ConfirmationDialog _confirmationDialog = null!;
	private Button _backgroundActionButton = null!;
	private Button _standardDialogButton = null!;
	private Button _dangerousDialogButton = null!;
	private Button _disabledDialogButton = null!;
	private Label _backgroundCountLabel = null!;
	private Label _statusLabel = null!;
	private string _activeDialog = string.Empty;
	private int _backgroundCount;
	private bool _highContrast;

	public override void _Ready()
	{
		_confirmationDialog =
			GetNode<ConfirmationDialog>("%DemoConfirmationDialog");
		_backgroundActionButton =
			GetNode<Button>("%BackgroundActionButton");
		_standardDialogButton =
			GetNode<Button>("%StandardDialogButton");
		_dangerousDialogButton =
			GetNode<Button>("%DangerousDialogButton");
		_disabledDialogButton =
			GetNode<Button>("%DisabledDialogButton");
		_backgroundCountLabel =
			GetNode<Label>("%BackgroundCountLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");

		_backgroundActionButton.Pressed += OnBackgroundActionPressed;
		_standardDialogButton.Pressed += ShowStandardDialog;
		_dangerousDialogButton.Pressed += ShowDangerousDialog;
		_disabledDialogButton.Pressed += ShowDisabledDialog;
		_confirmationDialog.Confirmed += OnConfirmed;
		_confirmationDialog.Cancelled += OnCancelled;
		_confirmationDialog.ResultSubmitted += OnResultSubmitted;

		_standardDialogButton.GrabFocus();
		UpdateStatus("Ready — open each confirmation type.");
	}

	public override void _Input(InputEvent inputEvent)
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

	private void OnBackgroundActionPressed()
	{
		_backgroundCount++;
		_backgroundCountLabel.Text =
			$"Background count: {_backgroundCount}";
		UpdateStatus("Background action activated.");
	}

	private void ShowStandardDialog()
	{
		_activeDialog = "standard rest confirmation";
		_confirmationDialog.ShowDialog(
			"Rest at camp?",
			"The party will consume supplies and advance time before continuing.",
			"Rest",
			"Keep traveling");
		UpdateStatus("Standard confirmation opened; Confirm begins focused.");
	}

	private void ShowDangerousDialog()
	{
		_activeDialog = "dangerous expedition confirmation";
		_confirmationDialog.ShowDialog(
			"Abandon the expedition?",
			"This deliberately long warning verifies wrapping at the minimum " +
			"supported resolution. Unsaved expedition progress will be lost, " +
			"and the party will return to the title screen.",
			"Abandon expedition",
			"Stay in adventure",
			ConfirmationDialogKind.Dangerous);
		UpdateStatus("Dangerous confirmation opened; Cancel begins focused.");
	}

	private void ShowDisabledDialog()
	{
		_activeDialog = "disabled protected-save confirmation";
		_confirmationDialog.ShowDialog(
			"Overwrite protected save?",
			"The protected save cannot be overwritten in this demonstration.",
			"Overwrite",
			"Cancel",
			ConfirmationDialogKind.Standard,
			confirmEnabled: false);
		UpdateStatus("Disabled confirmation opened; Confirm is unavailable.");
	}

	private void OnConfirmed()
	{
		UpdateStatus($"Confirmed: {_activeDialog}.");
	}

	private void OnCancelled()
	{
		UpdateStatus($"Cancelled: {_activeDialog}.");
	}

	private void OnResultSubmitted(bool confirmed)
	{
		GD.Print(
			$"Confirmation result: {(confirmed ? "confirmed" : "cancelled")}");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = message;
	}
}
