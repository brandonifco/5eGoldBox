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

		if (_inputRouter.Handle(
			keyEvent.Keycode,
			keyEvent.CtrlPressed,
			keyEvent.AltPressed))
		{
			GetViewport().SetInputAsHandled();
		}
	}
}
