// Mirrors the governing plan's §8.2 Escape/cancel priority list exactly,
// in priority order, so a CommandSetViewModel can name which step in that
// centralized ladder applies to it instead of each screen inventing its
// own description.
internal enum ShellCancelBehavior
{
	None,
	CloseTopmostConfirmation,
	StepBackModalScreen,
	CancelTargetingOrSelection,
	ExitDirectMovement,
	CloseNonmodalOverlay,
	OpenPauseMenu,
}
