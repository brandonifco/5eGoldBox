using System;
using Godot;

public partial class ConfirmationDialog : Control
{
	[Signal]
	public delegate void ConfirmedEventHandler();

	[Signal]
	public delegate void CancelledEventHandler();

	[Signal]
	public delegate void ResultSubmittedEventHandler(bool confirmed);

	[Export]
	public PackedScene DialogCardScene { get; set; } = null!;

	public bool IsOpen => _modalBackdrop.IsModalOpen;

	private ModalBackdrop _modalBackdrop = null!;
	private Button? _confirmButton;
	private Button? _cancelButton;

	public override void _Ready()
	{
		_modalBackdrop = GetNode<ModalBackdrop>("%DialogBackdrop");
		_modalBackdrop.DismissRequested += Cancel;
		_modalBackdrop.Closed += ClearDialogReferences;
	}

	public void ShowDialog(
		string title,
		string message,
		string confirmText = "Confirm",
		string cancelText = "Cancel",
		ConfirmationDialogKind kind = ConfirmationDialogKind.Standard,
		bool confirmEnabled = true)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(title);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);
		ArgumentException.ThrowIfNullOrWhiteSpace(confirmText);
		ArgumentException.ThrowIfNullOrWhiteSpace(cancelText);

		if (IsOpen)
		{
			Cancel();
		}

		PanelContainer card = DialogCardScene.Instantiate<PanelContainer>();
		Label titleLabel = card.GetNode<Label>("%TitleLabel");
		Label messageLabel = card.GetNode<Label>("%MessageLabel");
		_confirmButton = card.GetNode<Button>("%ConfirmButton");
		_cancelButton = card.GetNode<Button>("%CancelButton");

		titleLabel.Text = title;
		messageLabel.Text = message;
		_confirmButton.Text = confirmText;
		_confirmButton.Disabled = !confirmEnabled;
		_confirmButton.ThemeTypeVariation = kind == ConfirmationDialogKind.Dangerous
			? "ShellDangerousButton"
			: "ShellConfirmButton";
		_cancelButton.Text = cancelText;

		_confirmButton.Pressed += Confirm;
		_cancelButton.Pressed += Cancel;

		Control initialFocus =
			kind == ConfirmationDialogKind.Dangerous || !confirmEnabled
				? _cancelButton
				: _confirmButton;

		_modalBackdrop.ShowModal(card, initialFocus);
	}

	public void Confirm()
	{
		if (!IsOpen || _confirmButton is null || _confirmButton.Disabled)
		{
			return;
		}

		EmitSignal(SignalName.Confirmed);
		EmitSignal(SignalName.ResultSubmitted, true);
		_modalBackdrop.CloseModal();
	}

	public void Cancel()
	{
		if (!IsOpen)
		{
			return;
		}

		EmitSignal(SignalName.Cancelled);
		EmitSignal(SignalName.ResultSubmitted, false);
		_modalBackdrop.CloseModal();
	}

	public void CloseDialog()
	{
		Cancel();
	}

	private void ClearDialogReferences()
	{
		_confirmButton = null;
		_cancelButton = null;
	}
}
