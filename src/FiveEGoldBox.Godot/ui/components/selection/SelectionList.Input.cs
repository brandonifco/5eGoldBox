using Godot;

public partial class SelectionList
{
	private void ActivateIndex(int index)
	{
		if (!SelectIndex(index))
		{
			return;
		}

		EmitSignal(
			SignalName.SelectionActivated,
			index,
			_entries[index].Id);
	}

	private void HandleItemGuiInput(
		InputEvent inputEvent,
		int index)
	{
		if (inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		int targetIndex = keyEvent.Keycode switch
		{
			Key.Up => FindEnabledIndex(index - 1, -1),
			Key.Down => FindEnabledIndex(index + 1, 1),
			Key.Home => FindEnabledIndex(0, 1),
			Key.End => FindEnabledIndex(_entries.Count - 1, -1),
			_ => -1,
		};

		if (targetIndex >= 0)
		{
			SelectIndex(targetIndex, grabFocus: true);
			AcceptEvent();
		}
	}

	private int FindEnabledIndex(
		int startIndex,
		int direction)
	{
		for (
			int index = startIndex;
			index >= 0 && index < _entries.Count;
			index += direction)
		{
			if (!_entries[index].Disabled)
			{
				return index;
			}
		}

		return -1;
	}
}
