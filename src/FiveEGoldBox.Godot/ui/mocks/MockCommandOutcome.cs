// The four outcome categories from the governing plan's §15.2
// UiCommandResponse sketch, verbatim — reused here even though that type
// itself isn't authorized yet, because the categories themselves aren't
// gateway-shape decisions, they're what a command submission can actually
// result in.
internal enum MockCommandOutcome
{
	Accepted,
	RejectedForCurrentState,
	Busy,
	Error,
}
