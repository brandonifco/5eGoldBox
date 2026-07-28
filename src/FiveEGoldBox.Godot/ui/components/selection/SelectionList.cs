using System;
using System.Collections.Generic;
using Godot;

public partial class SelectionList : PanelContainer
{
	[Signal]
	public delegate void SelectionChangedEventHandler(
		int index,
		string itemId);

	[Signal]
	public delegate void SelectionActivatedEventHandler(
		int index,
		string itemId);

	[Export]
	public string TitleText { get; set; } = "Selection";

	[Export]
	public string EmptyText { get; set; } = "No entries available.";

	[Export]
	public bool AutoSelectFirst { get; set; } = true;

	private readonly List<SelectionListEntry> _entries = new();
	private readonly List<Button> _buttons = new();
	private Label _titleLabel = null!;
	private Label _emptyLabel = null!;
	private ScrollContainer _itemScroll = null!;
	private VBoxContainer _itemList = null!;
	private int _selectedIndex = -1;

	public int SelectedIndex => _selectedIndex;

	public string SelectedId => GetSelectedEntry()?.Id ?? string.Empty;

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_itemScroll = GetNode<ScrollContainer>("%ItemScroll");
		_itemList = GetNode<VBoxContainer>("%ItemList");

		ApplyText();
		RebuildButtons();
	}

	public void SetItems(IEnumerable<SelectionListEntry> entries)
	{
		ArgumentNullException.ThrowIfNull(entries);

		string previousId = SelectedId;
		_entries.Clear();
		HashSet<string> ids = new(StringComparer.Ordinal);

		foreach (SelectionListEntry entry in entries)
		{
			ValidateEntry(entry, ids);
			_entries.Add(entry);
		}

		if (IsNodeReady())
		{
			RebuildButtons(previousId);
		}
	}

	public void ClearItems()
	{
		SetItems(Array.Empty<SelectionListEntry>());
	}

	public SelectionListEntry? GetSelectedEntry()
	{
		return IsSelectableIndex(_selectedIndex)
			? _entries[_selectedIndex]
			: null;
	}

	public bool SelectIndex(
		int index,
		bool grabFocus = false)
	{
		if (!IsSelectableIndex(index))
		{
			return false;
		}

		bool changed = _selectedIndex != index;
		_selectedIndex = index;
		UpdateButtonStates();

		if (grabFocus)
		{
			_buttons[index].GrabFocus();
			_itemScroll.EnsureControlVisible(_buttons[index]);
		}

		if (changed)
		{
			EmitSignal(
				SignalName.SelectionChanged,
				index,
				_entries[index].Id);
		}

		return true;
	}

	public bool SelectId(
		string itemId,
		bool grabFocus = false)
	{
		for (int index = 0; index < _entries.Count; index++)
		{
			if (string.Equals(
				_entries[index].Id,
				itemId,
				StringComparison.Ordinal))
			{
				return SelectIndex(index, grabFocus);
			}
		}

		return false;
	}

	public void FocusSelected()
	{
		if (IsSelectableIndex(_selectedIndex))
		{
			SelectIndex(_selectedIndex, grabFocus: true);
		}
	}

	private void ApplyText()
	{
		_titleLabel.Text = TitleText;
		_titleLabel.Visible = !string.IsNullOrWhiteSpace(TitleText);
		_emptyLabel.Text = EmptyText;
	}

	private static void ValidateEntry(
		SelectionListEntry entry,
		HashSet<string> ids)
	{
		if (string.IsNullOrWhiteSpace(entry.Id))
		{
			throw new ArgumentException(
				"Selection-list item IDs cannot be empty.",
				nameof(entry));
		}

		if (!ids.Add(entry.Id))
		{
			throw new ArgumentException(
				$"Duplicate selection-list item ID: {entry.Id}",
				nameof(entry));
		}
	}

	private bool IsSelectableIndex(int index)
	{
		return index >= 0 &&
			index < _entries.Count &&
			!_entries[index].Disabled;
	}
}
