// Stable UiCommandIntent.CommandId values for direct-movement key
// presses (M6d) — the first place a real keypress constructs an actual
// UiCommandIntent rather than a raw display string.
internal static class ExplorationMovementCommandIds
{
	public const string MoveForward = "move-forward";
	public const string MoveBackward = "move-backward";
	public const string TurnLeft = "turn-left";
	public const string TurnRight = "turn-right";
}
