using System.Collections.Generic;

// One wizard screen, in the same Godot-owned view-model vocabulary
// (CommandViewModel) the rest of this client already renders through, so
// ModalScreenCard needs no new row type to draw it.
internal sealed record CharacterCreationStepView(
	CharacterCreationStep Step,
	string Title,
	string Breadcrumb,
	string BodyText,
	IReadOnlyList<CommandViewModel> Options,
	// Which options are currently chosen. One entry for a single-choice
	// step, however many are picked so far for a multi-select one -- the
	// caller renders a checkmark from this rather than tracking its own
	// copy of the selection.
	IReadOnlySet<string> SelectedOptionIds,
	bool IsMultiSelect,
	// A free-text step (the character's name) rather than a list one. The
	// two are mutually exclusive; Options is empty whenever this is true.
	bool IsTextEntry,
	string TextValue,
	// Whether "Next" may be taken from here yet, and why not when it may
	// not -- the wizard says what is missing rather than silently refusing.
	bool CanAdvance,
	string? BlockedReason);
