using Godot;

public partial class AppShell
{
	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (_inputRouter.Handle(keyEvent))
		{
			GetViewport().SetInputAsHandled();
		}
	}

	// Additive and joypad-only, deliberately kept off _UnhandledKeyInput's
	// path: _UnhandledInput fires for the whole tree before any node's
	// _UnhandledKeyInput does, so folding keyboard handling in here too
	// would run AppShell's Escape handling before ModalBackdrop's and
	// invert the cancel-priority order M4c established. See M4f's
	// milestone note on the modal/joypad priority gap this leaves open.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventJoypadButton { Pressed: true } joypadEvent)
		{
			return;
		}

		if (_inputRouter.Handle(joypadEvent))
		{
			GetViewport().SetInputAsHandled();
		}
	}
}
