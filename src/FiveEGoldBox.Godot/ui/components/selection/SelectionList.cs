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

	// Mouse hover alone, deliberately independent of SelectionChanged --
	// the row's own theme already supplies a distinct hover stylebox
	// (ShellSelectionRowButton/styles/hover), so the visual highlight is
	// free and this only needs to carry the "which row is the mouse over"
	// fact outward. Never touches _selectedIndex/UpdateButtonStates or
	// grabs focus: if it did, moving the mouse across the list while
	// navigating with the arrow keys would fight keyboard focus for which
	// row is "current," since a hovered row and a keyboard-focused row are
	// not the same thing and don't need to be kept in sync.
	[Signal]
	public delegate void RowHoveredEventHandler(
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
	private bool _compactLayout;
	private int _selectedIndex = -1;

	public int SelectedIndex => _selectedIndex;

	public string SelectedId => GetSelectedEntry()?.Id ?? string.Empty;

	// Character creation's own option lists (races, classes, ...) turn
	// this on; every other consumer (Character, Inventory, Spellbook, ...)
	// never sets it and is unaffected. Still the same vertical, one-row-
	// per-entry, scrollable-if-it-overflows list -- only each row's own
	// button changes, sized to its own text and left-aligned instead of
	// stretched to the container's full width (see CreateButton), with
	// this component's own panel background dropped so it reads as part
	// of whatever page it sits in rather than a nested boxed widget.
	//
	// An HFlowContainer (packing rows left-to-right, wrapping) was tried
	// first and reverted -- the user asked directly for "a vertically
	// oriented list," not options flowing sideways.
	public bool CompactLayout
	{
		get => _compactLayout;
		set
		{
			_compactLayout = value;

			if (IsNodeReady())
			{
				ApplyCompactStyling();
			}
		}
	}

	public override void _Ready()
	{
		_titleLabel = GetNode<Label>("%TitleLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_itemScroll = GetNode<ScrollContainer>("%ItemScroll");
		_itemList = GetNode<VBoxContainer>("%ItemList");

		ApplyText();
		ApplyCompactStyling();
		RebuildButtons();
	}

	// No fixed height of its own -- character creation (the only caller of
	// CompactLayout) reparents this into ModalScreenCard's own right
	// column and expects it to fill that column's height, which is what
	// TwoColumnRow's own fixed minimum actually controls. Imposing a
	// second, separate minimum here would just fight that instead of
	// cooperating with it.
	private void ApplyCompactStyling()
	{
		if (_compactLayout)
		{
			CustomMinimumSize = Vector2.Zero;
			SizeFlagsVertical = Control.SizeFlags.ExpandFill;
			AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
		}
		else
		{
			RemoveThemeStyleboxOverride("panel");
		}
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
