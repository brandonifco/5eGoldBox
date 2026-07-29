using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// The one card every M9 secondary screen (Character/Party, Inventory,
// Spellbook, Area Map, Journal, Options, Save/Load, and the reusable
// location-interaction screen) renders through — a single ModalViewModel
// shape (declared back at M5, never consumed until now) covers all of
// them: a title, optional body text, an optional list (reuses M4/M7's
// SelectionList rather than a second list component), and a row of
// action buttons (reuses HotkeyCommandButton, same as the live command
// bar, for keyboard/mouse parity here too).
public partial class ModalScreenCard : PanelContainer
{
	public event Action<string>? RowFocused;
	public event Action<string>? RowActivated;

	private Label _breadcrumbLabel = null!;
	private Label _titleLabel = null!;
	private Label _bodyLabel = null!;
	private SelectionList _itemList = null!;
	private HBoxContainer _commandRow = null!;
	private PackedScene _commandButtonScene = null!;
	private bool _nodesResolved;

	public Control? InitialFocusControl { get; private set; }

	public override void _Ready()
	{
		EnsureNodesResolved();
	}

	// Callers configure this card immediately after Instantiate(), before
	// it ever enters the SceneTree (see ModalScreenView.ShowScreen — it
	// needs the fully-configured card's InitialFocusControl before calling
	// ModalBackdrop.ShowModal), so this can't wait for _Ready() to resolve
	// node references the way most of this codebase's components do.
	// GetNode on a %unique-named child works pre-tree because Instantiate()
	// already wires node ownership; ConfirmationDialog.cs relies on the
	// exact same fact.
	private void EnsureNodesResolved()
	{
		if (_nodesResolved)
		{
			return;
		}

		_breadcrumbLabel = GetNode<Label>("%BreadcrumbLabel");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_bodyLabel = GetNode<Label>("%BodyLabel");
		_itemList = GetNode<SelectionList>("%ItemList");
		_commandRow = GetNode<HBoxContainer>("%CommandRow");
		_commandButtonScene = GD.Load<PackedScene>(
			"res://ui/components/commands/HotkeyCommandButton.tscn");

		_itemList.SelectionChanged += (_, itemId) => RowFocused?.Invoke(itemId);
		_itemList.SelectionActivated += (_, itemId) => RowActivated?.Invoke(itemId);
		_nodesResolved = true;
	}

	internal void Configure(
		ModalViewModel model,
		IReadOnlyDictionary<string, Action> commandHandlers)
	{
		EnsureNodesResolved();

		_breadcrumbLabel.Text = model.BreadcrumbText ?? string.Empty;
		_breadcrumbLabel.Visible = !string.IsNullOrWhiteSpace(model.BreadcrumbText);

		_titleLabel.Text = model.Title;

		_bodyLabel.Text = model.BodyText ?? string.Empty;
		_bodyLabel.Visible = !string.IsNullOrWhiteSpace(model.BodyText);

		bool hasItems = model.ListItems is { Count: > 0 };

		_itemList.Visible = hasItems;

		if (hasItems)
		{
			_itemList.SetItems(model.ListItems!.Select(
				item => new SelectionListEntry(
					item.CommandId,
					item.Label,
					Disabled: !item.Enabled)));

			if (model.SelectedRowId is not null)
			{
				_itemList.SelectId(model.SelectedRowId);
			}
		}
		else
		{
			_itemList.ClearItems();
		}

		RebuildCommandRow(model.Commands ?? Array.Empty<CommandViewModel>(), commandHandlers);

		InitialFocusControl = hasItems
			? _itemList
			: _commandRow.GetChildCount() > 0
				? _commandRow.GetChild<Control>(0)
				: null;
	}

	private void RebuildCommandRow(
		IReadOnlyList<CommandViewModel> commands,
		IReadOnlyDictionary<string, Action> commandHandlers)
	{
		foreach (Node child in _commandRow.GetChildren())
		{
			_commandRow.RemoveChild(child);
			child.QueueFree();
		}

		foreach (CommandViewModel command in commands)
		{
			if (!commandHandlers.TryGetValue(command.CommandId, out Action? handler))
			{
				throw new ArgumentException(
					$"No handler supplied for modal-screen command " +
						$"'{command.CommandId}'.",
					nameof(commandHandlers));
			}

			CommandDefinition definition =
				CommandViewModelTranslator.ToCommandDefinition(command, handler);
			HotkeyCommandButton button =
				_commandButtonScene.Instantiate<HotkeyCommandButton>();

			button.Configure(
				definition.Name,
				definition.FormattedLabel,
				definition.ShortcutKey,
				definition.Handler,
				enableShortcut: true);
			button.Disabled = !command.Enabled;

			_commandRow.AddChild(button);
		}
	}
}
