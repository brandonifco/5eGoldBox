using System.Collections.Generic;

internal sealed record ModalViewModel(
	string Title,
	string? BodyText = null,
	// Reuses CommandViewModel's shape for both fields rather than a
	// near-duplicate row type — a selectable list row and an action
	// button are both just an id/label/enabled/tooltip to the UI. They
	// stay separate fields because the governing plan (§7.2) lists list
	// content and commands as distinct concepts (rows to pick vs. actions
	// to take), not because their shape actually differs.
	IReadOnlyList<CommandViewModel>? ListItems = null,
	IReadOnlyList<CommandViewModel>? Commands = null,
	string? SelectedRowId = null,
	string? BreadcrumbText = null,
	// A real boxed stat-block, not text -- distinct from BodyText because
	// the card renders it as a whole other layout (a grid, not a label),
	// not just formatted differently. Null for every screen but Character.
	CharacterSheetViewModel? CharacterSheet = null,
	// A free-text field, for the one thing a player types rather than
	// picks: their character's name. Null everywhere else, and mutually
	// exclusive with ListItems in practice -- a step either offers choices
	// or asks for text, never both.
	ModalTextEntryViewModel? TextEntry = null);

internal sealed record ModalTextEntryViewModel(
	string InitialValue,
	string PlaceholderText);
