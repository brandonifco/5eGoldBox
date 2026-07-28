using Godot;

public partial class ComponentGallery
{
	private void ConfigureModalExamples()
	{
		_openModalButton.Pressed += ShowGenericModal;
		_standardConfirmationButton.Pressed += ShowStandardConfirmation;
		_dangerousConfirmationButton.Pressed += ShowDangerousConfirmation;
		_disabledConfirmationButton.Pressed += ShowDisabledConfirmation;

		_modalBackdrop.DismissRequested += () =>
			UpdateStatus(
				"ModalBackdrop dismissal requested; use the Close button.");
		_modalBackdrop.Opened += () =>
			UpdateStatus("ModalBackdrop opened and background input is blocked.");
		_modalBackdrop.Closed += () =>
			UpdateStatus("ModalBackdrop closed and focus was restored.");

		_confirmationDialog.Confirmed += () =>
			UpdateStatus("ConfirmationDialog submitted a positive result.");
		_confirmationDialog.Cancelled += () =>
			UpdateStatus("ConfirmationDialog submitted a cancelled result.");
	}

	private void ShowGenericModal()
	{
		Control card = ModalCardScene.Instantiate<Control>();
		Button modalAction = card.GetNode<Button>("%ModalActionButton");
		Button closeButton = card.GetNode<Button>("%CloseButton");

		modalAction.Pressed += () =>
			UpdateStatus("Generic modal action activated.");
		closeButton.Pressed += _modalBackdrop.CloseModal;
		_modalBackdrop.ShowModal(card, modalAction);
	}

	private void ShowStandardConfirmation()
	{
		_confirmationDialog.ShowDialog(
			"Standard confirmation",
			"Confirm begins focused and both mouse and keyboard activation are supported.",
			"Confirm",
			"Cancel");
	}

	private void ShowDangerousConfirmation()
	{
		_confirmationDialog.ShowDialog(
			"Dangerous confirmation",
			"This deliberately long warning verifies safe initial focus, " +
			"dangerous-action styling, wrapping, and minimum-resolution behavior.",
			"Proceed anyway",
			"Return safely",
			ConfirmationDialogKind.Dangerous);
	}

	private void ShowDisabledConfirmation()
	{
		_confirmationDialog.ShowDialog(
			"Disabled confirmation",
			"The positive action is unavailable in this component state.",
			"Unavailable",
			"Close",
			ConfirmationDialogKind.Standard,
			confirmEnabled: false);
	}
}
