// The complete interaction-context taxonomy for M4. CommandMenu and
// ExplorationMovement are wired to a real producer today; ListSelection,
// Targeting, and Modal are declared now so M6/M8/M9's first list, target
// picker, and dialog have a context to push onto the stack, rather than
// each milestone inventing its own naming.
internal enum ShellInteractionContext
{
	CommandMenu,
	ExplorationMovement,
	ListSelection,
	Targeting,
	Modal,
}
