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
	string? BreadcrumbText = null);
