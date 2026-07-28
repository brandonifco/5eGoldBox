// The governing plan's §7.3 command intent contract, verbatim field
// list. Every player action should be expressed as one of these instead
// of a button calling a future backend method directly — a mock handler
// consumes it before integration, an adapter after. The UI may validate
// input shape (e.g. requiring a selection before emitting one); it must
// never validate game legality.
internal sealed record UiCommandIntent(
	string CommandId,
	string? ContextId = null,
	string? SubjectId = null,
	string? TargetId = null,
	IUiCommandPayload? Payload = null);
