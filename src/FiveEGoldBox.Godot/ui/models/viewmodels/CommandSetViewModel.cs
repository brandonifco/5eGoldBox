using System.Collections.Generic;

internal sealed record CommandSetViewModel(
	IReadOnlyList<CommandViewModel> Commands,
	ShellInteractionContext InteractionMode,
	ShellCancelBehavior CancelBehavior = ShellCancelBehavior.None,
	string? PromptText = null);
