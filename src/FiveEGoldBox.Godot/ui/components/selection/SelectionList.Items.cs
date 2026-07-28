using Godot;

public partial class SelectionList
{
	private void RebuildButtons(string preferredId = "")
	{
		foreach (Node child in _itemList.GetChildren())
		{
			_itemList.RemoveChild(child);
			child.QueueFree();
		}

		_buttons.Clear();
		_selectedIndex = -1;

		for (int index = 0; index < _entries.Count; index++)
		{
			int itemIndex = index;
			SelectionListEntry entry = _entries[index];
			Button button = CreateButton(entry, itemIndex);
			_buttons.Add(button);
			_itemList.AddChild(button);
		}

		bool hasItems = _entries.Count > 0;
		_emptyLabel.Visible = !hasItems;
		_itemScroll.Visible = hasItems;

		if (!string.IsNullOrEmpty(preferredId) &&
			SelectId(preferredId))
		{
			return;
		}

		if (AutoSelectFirst)
		{
			int firstIndex = FindEnabledIndex(0, 1);

			if (firstIndex >= 0)
			{
				SelectIndex(firstIndex);
			}
		}
	}

	private Button CreateButton(
		SelectionListEntry entry,
		int index)
	{
		Button button = new()
		{
			Name = $"SelectionItem{index + 1}",
			Text = entry.Text,
			TooltipText = entry.Text,
			ThemeTypeVariation = "ShellSelectionRowButton",
			ToggleMode = true,
			Disabled = entry.Disabled,
			FocusMode = entry.Disabled
				? Control.FocusModeEnum.None
				: Control.FocusModeEnum.All,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0, 42),
			Alignment = HorizontalAlignment.Left,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};

		button.FocusEntered += () => SelectIndex(index);
		button.Pressed += () => ActivateIndex(index);
		button.GuiInput += inputEvent =>
			HandleItemGuiInput(inputEvent, index);

		return button;
	}

	private void UpdateButtonStates()
	{
		for (int index = 0; index < _buttons.Count; index++)
		{
			_buttons[index].ButtonPressed =
				index == _selectedIndex;
		}
	}
}
