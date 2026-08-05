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

	// The Character screen's own boxed stat-block -- a real grid layout,
	// not text, which is why it's a separate section rather than more
	// BodyLabel formatting. Hidden for every other screen.
	private VBoxContainer _characterSheetPanel = null!;
	private Label _characterNameLabel = null!;
	private Label _characterSubtitleLabel = null!;
	private GridContainer _abilityGrid = null!;
	private Label _statsLabel = null!;
	private Label _encumbranceLabel = null!;
	private Label _purseLabel = null!;

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

		_characterSheetPanel = GetNode<VBoxContainer>("%CharacterSheetPanel");
		_characterNameLabel = GetNode<Label>("%CharacterNameLabel");
		_characterSubtitleLabel = GetNode<Label>("%CharacterSubtitleLabel");
		_abilityGrid = GetNode<GridContainer>("%AbilityGrid");
		_statsLabel = GetNode<Label>("%StatsLabel");
		_encumbranceLabel = GetNode<Label>("%EncumbranceLabel");
		_purseLabel = GetNode<Label>("%PurseLabel");

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

		_characterSheetPanel.Visible = model.CharacterSheet is not null;

		if (model.CharacterSheet is not null)
		{
			ApplyCharacterSheet(model.CharacterSheet);
		}

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
	}

	// SelectionList itself has FocusMode.None — only its internal row
	// buttons are focusable — so landing focus on "the list" means asking
	// it to focus its own selected row (FocusSelected, M4/M7) rather than
	// grabbing focus on the container directly, which Godot logs as a
	// silent no-op warning ("this control can't grab focus") rather than
	// an error, easy to miss without checking stderr.
	internal void FocusInitialControl()
	{
		if (_itemList.Visible)
		{
			_itemList.FocusSelected();
			return;
		}

		if (_commandRow.GetChildCount() > 0)
		{
			_commandRow.GetChild<Control>(0).GrabFocus();
		}
	}

	// A lighter update for a list-row focus change alone (e.g. Character's
	// list picking a different party member to describe) — reuses M7f's
	// precedent (RegionalMapView.SetSelectedLocation) of not tearing down
	// and reopening the whole screen just to change what one line of text
	// says.
	internal void SetBodyText(string? bodyText)
	{
		EnsureNodesResolved();

		_bodyLabel.Text = bodyText ?? string.Empty;
		_bodyLabel.Visible = !string.IsNullOrWhiteSpace(bodyText);
	}

	private void ApplyCharacterSheet(CharacterSheetViewModel sheet)
	{
		_characterNameLabel.Text = sheet.Name;
		_characterSubtitleLabel.Text = sheet.RaceClassLevel;
		_statsLabel.Text =
			$"AC {sheet.ArmorClass}   {sheet.HealthText}   " +
				$"Speed {sheet.SpeedFeet} ft.";

		_encumbranceLabel.Text = sheet.EncumbranceText;
		// Reuses the existing warning color rather than a new style --
		// same theme entry ShellStatusWarning already applies elsewhere
		// for an at-a-glance-bad state.
		_encumbranceLabel.ThemeTypeVariation = sheet.IsEncumbered
			? "ShellStatusWarning"
			: "ShellBodyLabel";

		_purseLabel.Text = sheet.PurseText;

		foreach (Node child in _abilityGrid.GetChildren())
		{
			_abilityGrid.RemoveChild(child);
			child.QueueFree();
		}

		foreach (CharacterSheetAbilityViewModel ability in sheet.AbilityScores)
		{
			_abilityGrid.AddChild(new Label
			{
				Text = $"{ability.Abbreviation} {ability.Score} " +
					$"({ability.Modifier:+0;-0})",
				ThemeTypeVariation = "ShellBodyLabel",
			});
		}
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
