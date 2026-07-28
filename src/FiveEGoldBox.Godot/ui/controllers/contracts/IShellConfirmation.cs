using System;

internal interface IShellConfirmation
{
	void ShowConfirmation(
		string title,
		string message,
		string confirmText,
		string cancelText,
		Action onConfirmed,
		Action onClosed);
}
