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
			Alignment = HorizontalAlignment.Left,
		};

		if (_compactLayout)
		{
			// Left-aligned and sized to its own text instead of stretched
			// to the row's full width (ShrinkBegin, not the default Fill
			// that made every row span edge to edge) -- still one row per
			// entry, top to bottom, just a narrower row. AutowrapMode has
			// to stay Off here: with word-wrap on and no minimum width
			// imposed, the container sized every button down to just its
			// narrowest possible wrapped line, collapsing all of them into
			// unreadable slivers -- confirmed live, not guessed at.
			button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
			button.CustomMinimumSize = new Vector2(0, 34);
			button.AutowrapMode = TextServer.AutowrapMode.Off;
			button.AddThemeFontSizeOverride("font_size", 14);
		}
		else
		{
			button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
			button.CustomMinimumSize = new Vector2(0, 42);
			button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		}

		button.FocusEntered += () => SelectIndex(index);
		button.Pressed += () => ActivateIndex(index);
		button.GuiInput += inputEvent =>
			HandleItemGuiInput(inputEvent, index);
		button.MouseEntered += () => HandleRowHovered(index);

		return button;
	}

	// See RowHovered's own comment (SelectionList.cs) for why this stays
	// separate from SelectIndex rather than calling it.
	private void HandleRowHovered(int index)
	{
		if (!IsSelectableIndex(index))
		{
			return;
		}

		EmitSignal(SignalName.RowHovered, index, _entries[index].Id);
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
