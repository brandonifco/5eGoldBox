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

	// Fires as the player types, so a wizard step can enable or disable its
	// own "Next" against what is currently in the field rather than only
	// finding out once they try to leave.
	public event Action<string>? TextChanged;

	// Enter in the text field means the same thing as activating a row does
	// elsewhere: take the step's primary action.
	public event Action? TextSubmitted;

	private Label _breadcrumbLabel = null!;
	private Label _titleLabel = null!;
	// BodyLabel sits inside its own fixed-height ScrollContainer, the same
	// pattern ItemList already uses -- a ScrollContainer never reports its
	// content's full height as its own minimum size, so however long the
	// current description is, the label can grow freely inside it without
	// changing BodyScroll's own size and shifting everything below it.
	// Letting the label itself drive the outer layout (its old shape) was
	// what caused the whole card to visibly resize and re-center every
	// time a longer or shorter option was hovered.
	private ScrollContainer _bodyScroll = null!;
	private Label _bodyLabel = null!;
	private SelectionList _itemList = null!;
	private HBoxContainer _commandRow = null!;
	private PackedScene _commandButtonScene = null!;
	private bool _nodesResolved;

	// Character creation's two-column layout: BodyScroll and ItemList are
	// reparented in here (from their normal stacked positions directly in
	// ScreenLayout) when CompactOptionsLayout is set, so the description
	// and the option list each get the row's full height instead of
	// splitting one shared vertical budget between them -- which is what
	// kept forcing either one or the other to scroll no matter how the
	// numbers were tuned. Every other screen leaves this hidden and never
	// touches it, so BodyScroll/ItemList stay in their original stacked
	// position for them, completely unaffected.
	private HBoxContainer _twoColumnRow = null!;
	private VBoxContainer _leftColumn = null!;
	private VBoxContainer _rightColumn = null!;

	// The Character screen's own boxed stat-block -- a real grid layout,
	// not text, which is why it's a separate section rather than more
	// BodyLabel formatting. Hidden for every other screen.
	private LineEdit _textEntryField = null!;
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
		_bodyScroll = GetNode<ScrollContainer>("%BodyScroll");
		_bodyLabel = GetNode<Label>("%BodyLabel");
		_itemList = GetNode<SelectionList>("%ItemList");
		_twoColumnRow = GetNode<HBoxContainer>("%TwoColumnRow");
		_leftColumn = GetNode<VBoxContainer>("%LeftColumn");
		_rightColumn = GetNode<VBoxContainer>("%RightColumn");
		_commandRow = GetNode<HBoxContainer>("%CommandRow");
		_textEntryField = GetNode<LineEdit>("%TextEntryField");
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
		// Mouse hover drives the exact same RowFocused callback keyboard
		// navigation already does -- a caller showing a description on
		// focus (CharacterCreation's HandleCreationRowFocused) gets
		// hover-to-preview for free, with no separate wiring needed.
		_itemList.RowHovered += (_, itemId) => RowFocused?.Invoke(itemId);
		_itemList.SelectionActivated += (_, itemId) => RowActivated?.Invoke(itemId);
		_textEntryField.TextChanged += text => TextChanged?.Invoke(text);
		_textEntryField.TextSubmitted += _ => TextSubmitted?.Invoke();
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
		_bodyScroll.Visible = !string.IsNullOrWhiteSpace(model.BodyText);

		bool hasItems = model.ListItems is { Count: > 0 };

		// Every CompactOptionsLayout step reserves the same two-column
		// height now, whether or not it actually has a right-column list --
		// deliberately not gated on hasItems the way this first shipped.
		// Character creation is the one caller of CompactOptionsLayout, and
		// it moves between steps with a list (Race, Class, Skills...) and
		// steps without one (Name, Review, and Assign Ability Scores' own
		// "all six assigned" summary) constantly. Gating the reservation on
		// hasItems meant the card's whole height -- and with it, the Back/
		// Next/Cancel row's screen position -- changed under the player's
		// feet the instant a step's option list emptied out. Confirmed
		// live: finishing ability-score assignment (a list-bearing step
		// collapsing to a bare summary) moved the button row by roughly
		// 190px, so a player clicking "Next" at the position that had
		// worked for the five score placements just before it missed
		// entirely, landed on the game view behind the now-smaller modal,
		// and silently abandoned the whole party build with no
		// confirmation. A little empty space on the right of a no-list
		// step is a much smaller cost than that.
		//
		// Reparenting must happen before _itemList.CompactLayout/SetItems
		// below, though it doesn't strictly depend on that order -- kept
		// together for clarity. A fresh ModalScreenCard is instantiated
		// per ShowScreen call (see ModalScreenView), so this always starts
		// from the pristine .tscn-authored position -- no "undo the
		// reparent" branch is needed for a step that doesn't use it.
		bool useTwoColumns = model.CompactOptionsLayout;

		_twoColumnRow.Visible = useTwoColumns;

		if (useTwoColumns)
		{
			_bodyScroll.Reparent(_leftColumn);
			_bodyScroll.CustomMinimumSize = Vector2.Zero;
			_bodyScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
			_itemList.Reparent(_rightColumn);
		}

		_characterSheetPanel.Visible = model.CharacterSheet is not null;

		if (model.CharacterSheet is not null)
		{
			ApplyCharacterSheet(model.CharacterSheet);
		}

		_textEntryField.Visible = model.TextEntry is not null;

		if (model.TextEntry is not null)
		{
			_textEntryField.Text = model.TextEntry.InitialValue;
			_textEntryField.PlaceholderText = model.TextEntry.PlaceholderText;

			// Put the caret after any text carried back in (stepping back
			// onto an already-named character), rather than before it.
			_textEntryField.CaretColumn = model.TextEntry.InitialValue.Length;

			// Same reservation as BodyScroll/ItemList just above -- the
			// Name step is a CompactOptionsLayout step with no list, so its
			// field belongs in the same reserved left column its prompt
			// text renders in, not floating in its own row below a mostly-
			// empty two-column area.
			if (useTwoColumns)
			{
				_textEntryField.Reparent(_leftColumn);
			}
		}

		// Set before SetItems -- RebuildButtons (triggered by SetItems)
		// reads this to decide which container new rows go into, so it has
		// to already be correct by the time items are actually built.
		_itemList.CompactLayout = model.CompactOptionsLayout;
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
		// A text step is asking the player to type; landing focus anywhere
		// but the field would make them tab to it first.
		if (_textEntryField.Visible)
		{
			_textEntryField.GrabFocus();
			return;
		}

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
		_bodyScroll.Visible = !string.IsNullOrWhiteSpace(bodyText);

		// The label's own scroll position doesn't reset just because its
		// text changed -- without this, hovering from a long description
		// to a short one can leave the (now-empty) view scrolled past
		// what little text is there, reading as blank.
		_bodyScroll.ScrollVertical = 0;
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

	// Lets a caller refresh just the command row -- specifically, whether
	// "Next" is enabled -- without the full Configure() a text-entry step
	// would otherwise need on every keystroke. Configure() rebuilds the
	// whole card layout (two-column reparenting, item list, character
	// sheet); calling it per keystroke would fight the text field for
	// focus while the player is still typing. RebuildCommandRow only
	// touches the button row, so this is safe to call as often as the
	// underlying step's CanAdvance might have changed.
	internal void UpdateCommands(
		IReadOnlyList<CommandViewModel> commands,
		IReadOnlyDictionary<string, Action> commandHandlers)
	{
		EnsureNodesResolved();
		RebuildCommandRow(commands, commandHandlers);
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
