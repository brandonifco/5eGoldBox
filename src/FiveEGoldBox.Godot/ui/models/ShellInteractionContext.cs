// Named to match the "Primary interaction" state machine in the
// governing plan (§7.1) exactly, not invented independently — read after
// M4b had already shipped with different names (ExplorationMovement,
// ListSelection, a single collapsed Modal) and corrected here before M5
// builds anything on top of the wrong vocabulary. CommandMenu and
// DirectMovement are wired to a real producer today; SelectionList,
// Targeting, Confirmation, and ModalScreen are declared now so M6/M8/M9's
// first list, target picker, and dialog have a context to push onto the
// stack, rather than each milestone inventing its own naming.
internal enum ShellInteractionContext
{
	CommandMenu,
	DirectMovement,
	SelectionList,
	Targeting,
	Confirmation,
	ModalScreen,
}
