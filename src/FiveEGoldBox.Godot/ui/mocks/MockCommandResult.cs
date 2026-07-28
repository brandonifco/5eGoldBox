internal sealed record MockCommandResult(
	MockCommandOutcome Outcome,
	ShellViewModel? UpdatedSnapshot = null,
	string? Message = null)
{
	public static MockCommandResult Accepted(
		ShellViewModel updatedSnapshot,
		string? message = null)
	{
		return new MockCommandResult(
			MockCommandOutcome.Accepted,
			updatedSnapshot,
			message);
	}

	public static MockCommandResult RejectedForCurrentState(string message)
	{
		return new MockCommandResult(
			MockCommandOutcome.RejectedForCurrentState,
			Message: message);
	}

	public static MockCommandResult Busy(string? message = null)
	{
		return new MockCommandResult(MockCommandOutcome.Busy, Message: message);
	}

	public static MockCommandResult Error(string message)
	{
		return new MockCommandResult(MockCommandOutcome.Error, Message: message);
	}
}
