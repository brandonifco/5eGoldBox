using System;

// Wraps ConfirmationDialog (M3, previously only exercised in dev demos —
// this is its first use in the live shell). Constructed independently of
// ShellInteractionController on purpose: that controller owns the
// interaction-context stack and needs to push/pop Confirmation around
// whatever it shows, so the callbacks flow from there into this
// controller, not the other way — avoids a construction-order cycle.
internal sealed class ShellConfirmationController : IShellConfirmation
{
	private readonly ConfirmationDialog _dialog;
	private Action? _onConfirmed;
	private Action? _onClosed;

	public ShellConfirmationController(ConfirmationDialog dialog)
	{
		_dialog = dialog;
		_dialog.Confirmed += HandleConfirmed;
		_dialog.ResultSubmitted += HandleResultSubmitted;
	}

	public void ShowConfirmation(
		string title,
		string message,
		string confirmText,
		string cancelText,
		Action onConfirmed,
		Action onClosed)
	{
		_onConfirmed = onConfirmed;
		_onClosed = onClosed;
		_dialog.ShowDialog(title, message, confirmText, cancelText);
	}

	private void HandleConfirmed()
	{
		_onConfirmed?.Invoke();
	}

	private void HandleResultSubmitted(bool confirmed)
	{
		_onClosed?.Invoke();
	}
}
