using Godot;

public partial class ModalBackdropDemo : Control
{
	[Export]
	public Theme StandardTheme { get; set; } = null!;

	[Export]
	public Theme HighContrastTheme { get; set; } = null!;

	[Export]
	public PackedScene ModalCardScene { get; set; } = null!;

	private ModalBackdrop _modalBackdrop = null!;
	private Button _backgroundActionButton = null!;
	private Button _openModalButton = null!;
	private Label _backgroundCountLabel = null!;
	private Label _statusLabel = null!;
	private int _backgroundCount;
	private bool _highContrast;

	public override void _Ready()
	{
		_modalBackdrop = GetNode<ModalBackdrop>("%DemoModalBackdrop");
		_backgroundActionButton = GetNode<Button>("%BackgroundActionButton");
		_openModalButton = GetNode<Button>("%OpenModalButton");
		_backgroundCountLabel = GetNode<Label>("%BackgroundCountLabel");
		_statusLabel = GetNode<Label>("%StatusLabel");

		_backgroundActionButton.Pressed += OnBackgroundActionPressed;
		_openModalButton.Pressed += OpenModal;
		_modalBackdrop.DismissRequested += OnDismissRequested;
		_modalBackdrop.Opened += () =>
			UpdateStatus("Modal opened; background input is blocked.");
		_modalBackdrop.Closed += () =>
			UpdateStatus("Modal closed; focus returns to Open modal.");

		_openModalButton.GrabFocus();
		UpdateStatus("Ready — test the background action, then open the modal.");
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

	private void OpenModal()
	{
		Control card = ModalCardScene.Instantiate<Control>();
		Button modalAction = card.GetNode<Button>("%ModalActionButton");
		Button closeButton = card.GetNode<Button>("%CloseButton");

		modalAction.Pressed += () =>
			UpdateStatus("Modal action activated without closing.");
		closeButton.Pressed += _modalBackdrop.CloseModal;

		_modalBackdrop.ShowModal(card, modalAction);
	}

	private void OnDismissRequested()
	{
		UpdateStatus(
			"Dismissal requested and intercepted; use Close modal in this demo.");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = message;
	}
}
