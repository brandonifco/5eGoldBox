using System;
using System.Collections.Generic;

// Named Mock* throughout, deliberately not IGameUiGateway/UiSnapshot/
// UiCommandResponse — the governing plan's §15.2 explicitly reserves
// those exact names for the real integration port and says their method
// shape "is not authorized by this document; it must be finalized only
// after the UI flows and Application public surface are both stable."
// This is the pre-integration stand-in §6.1's layer model calls the
// "Mock UI gateway," built now so M6-M9 have something to develop
// against, not an attempt to preempt that later authorization.
internal sealed class MockUiGateway
{
	private readonly Dictionary<string, Func<UiCommandIntent, MockCommandResult>>
		_handlers = new();

	private ShellViewModel _snapshot;

	public MockUiGateway(ShellViewModel initialSnapshot)
	{
		_snapshot = initialSnapshot;
	}

	public event Action<ShellViewModel>? SnapshotChanged;

	public ShellViewModel CurrentSnapshot => _snapshot;

	public void RegisterHandler(
		string commandId,
		Func<UiCommandIntent, MockCommandResult> handler)
	{
		_handlers[commandId] = handler;
	}

	public void ReplaceSnapshot(ShellViewModel snapshot)
	{
		_snapshot = snapshot;
		SnapshotChanged?.Invoke(_snapshot);
	}

	public MockCommandResult Submit(UiCommandIntent intent)
	{
		if (!_handlers.TryGetValue(intent.CommandId, out var handler))
		{
			return MockCommandResult.RejectedForCurrentState(
				$"No mock handler registered for '{intent.CommandId}'.");
		}

		MockCommandResult result = handler(intent);

		if (result.UpdatedSnapshot is not null)
		{
			ReplaceSnapshot(result.UpdatedSnapshot);
		}

		return result;
	}
}
