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

		int targetIndex = FindTargetIndex(keyEvent, index);

		if (targetIndex >= 0)
		{
			SelectIndex(targetIndex, grabFocus: true);
			AcceptEvent();
		}
	}

	private int FindTargetIndex(InputEventKey keyEvent, int index)
	{
		if (keyEvent.IsActionPressed(PlayerInputActions.UiUp))
		{
			return FindEnabledIndex(index - 1, -1);
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.UiDown))
		{
			return FindEnabledIndex(index + 1, 1);
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.ListSelectFirst))
		{
			return FindEnabledIndex(0, 1);
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.ListSelectLast))
		{
			return FindEnabledIndex(_entries.Count - 1, -1);
		}

		return -1;
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
