using System;
using System.Collections.Generic;
using Godot;

public partial class ModalBackdrop : Control
{
	[Signal]
	public delegate void DismissRequestedEventHandler();

	[Signal]
	public delegate void OpenedEventHandler();

	[Signal]
	public delegate void ClosedEventHandler();

	[Export]
	public bool DismissOnBackdropPressed { get; set; }

	[Export]
	public bool DismissOnEscape { get; set; }

	public bool IsModalOpen => Visible;

	private PanelContainer _backdropSurface = null!;
	// A MarginContainer, not a CenterContainer -- the modal card should
	// fill the safe area rather than shrink-wrap to its own content and
	// float in the middle of a lot of unused space (the actual complaint
	// that prompted this: character creation's step body/list wanted more
	// room than a fixed-width centered card gave it).
	private MarginContainer _contentHost = null!;
	private Control? _previousFocus;

	public override void _Ready()
	{
		_backdropSurface = GetNode<PanelContainer>("%BackdropSurface");
		_contentHost = GetNode<MarginContainer>("%ContentHost");
		_backdropSurface.GuiInput += OnBackdropGuiInput;
		Hide();
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!Visible ||
			inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo ||
			!IsFocusCycleKey(keyEvent))
		{
			return;
		}

		MoveFocus(keyEvent.ShiftPressed ? -1 : 1);
		GetViewport().SetInputAsHandled();
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (!Visible ||
			inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.UiCancel))
		{
			EmitSignal(SignalName.DismissRequested);

			if (DismissOnEscape)
			{
				CloseModal();
			}
		}

		GetViewport().SetInputAsHandled();
	}

	private static bool IsFocusCycleKey(InputEventKey keyEvent)
	{
		return keyEvent.IsActionPressed(
				PlayerInputActions.UiFocusNext,
				exactMatch: true) ||
			keyEvent.IsActionPressed(
				PlayerInputActions.UiFocusPrev,
				exactMatch: true);
	}

	public Control? GetModalContent()
	{
		return _contentHost.GetChildCount() == 0
			? null
			: _contentHost.GetChild<Control>(0);
	}

	public void SetModalContent(Control content)
	{
		ArgumentNullException.ThrowIfNull(content);

		if (content.GetParent() is not null)
		{
			throw new InvalidOperationException(
				"Modal content must not already have a parent.");
		}

		ClearModalContent();
		_contentHost.AddChild(content);
	}

	public void ClearModalContent()
	{
		foreach (Node child in _contentHost.GetChildren())
		{
			_contentHost.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void ShowModal(
		Control content,
		Control? initialFocus = null)
	{
		_previousFocus = GetViewport().GuiGetFocusOwner();
		SetModalContent(content);
		Show();
		MoveToFront();

		Control focusTarget = initialFocus ?? GetFirstFocusableControl() ?? this;
		focusTarget.CallDeferred(Control.MethodName.GrabFocus);
		EmitSignal(SignalName.Opened);
	}

	public void HideModal()
	{
		if (!Visible)
		{
			return;
		}

		Hide();
		RestorePreviousFocus();
		EmitSignal(SignalName.Closed);
	}

	public void CloseModal()
	{
		HideModal();
		ClearModalContent();
	}

	private void OnBackdropGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton ||
			!mouseButton.Pressed ||
			mouseButton.ButtonIndex != MouseButton.Left)
		{
			return;
		}

		EmitSignal(SignalName.DismissRequested);

		if (DismissOnBackdropPressed)
		{
			CloseModal();
		}

		GetViewport().SetInputAsHandled();
	}

	private void MoveFocus(int direction)
	{
		List<Control> focusableControls = GetFocusableControls();

		if (focusableControls.Count == 0)
		{
			GrabFocus();
			return;
		}

		Control? currentFocus = GetViewport().GuiGetFocusOwner();
		int currentIndex = currentFocus is null
			? -1
			: focusableControls.IndexOf(currentFocus);
		int nextIndex;

		if (currentIndex < 0)
		{
			nextIndex = direction > 0
				? 0
				: focusableControls.Count - 1;
		}
		else
		{
			nextIndex = (currentIndex + direction + focusableControls.Count) %
				focusableControls.Count;
		}

		focusableControls[nextIndex].GrabFocus();
	}

	private Control? GetFirstFocusableControl()
	{
		List<Control> controls = GetFocusableControls();
		return controls.Count == 0 ? null : controls[0];
	}

	private List<Control> GetFocusableControls()
	{
		List<Control> controls = new();
		CollectFocusableControls(_contentHost, controls);
		return controls;
	}

	private static void CollectFocusableControls(
		Node parent,
		List<Control> controls)
	{
		foreach (Node child in parent.GetChildren())
		{
			if (child is Control control &&
				control.IsVisibleInTree() &&
				control.FocusMode != Control.FocusModeEnum.None &&
				(control is not BaseButton button || !button.Disabled))
			{
				controls.Add(control);
			}

			CollectFocusableControls(child, controls);
		}
	}

	private void RestorePreviousFocus()
	{
		if (_previousFocus is not null &&
			GodotObject.IsInstanceValid(_previousFocus) &&
			_previousFocus.IsInsideTree())
		{
			_previousFocus.CallDeferred(Control.MethodName.GrabFocus);
		}

		_previousFocus = null;
	}
}
