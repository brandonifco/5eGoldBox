// A fixed, named, deterministic script — exploration -> regional map ->
// combat -> exploration, with a modal opened and closed along the way —
// built entirely on MockUiGateway's existing Submit/RegisterHandler/
// SnapshotChanged plumbing from M5b. Nothing new was added to the
// gateway itself; this is content riding the mechanism, exactly what
// "scripted mock transitions" (§11.6/M5e) asks for.
internal sealed class MockScriptedSession
{
	public const string Travel = "mock.travel";
	public const string EnterCombat = "mock.enter-combat";
	public const string FinishCombat = "mock.finish-combat";
	public const string OpenInventory = "mock.open-inventory";
	public const string CloseModal = "mock.close-modal";
	public const string SlowCommand = "mock.slow-command";
	public const string RiskyAction = "mock.risky-action";

	public static readonly string[] ScriptOrder =
	{
		Travel,
		EnterCombat,
		FinishCombat,
		OpenInventory,
		CloseModal,
		SlowCommand,
		SlowCommand,
		RiskyAction,
		RiskyAction,
	};

	public MockUiGateway Gateway { get; }

	public MockScriptedSession()
	{
		Gateway = new MockUiGateway(MockFullScenarios.Exploration());

		Gateway.RegisterHandler(
			Travel,
			_ => MockCommandResult.Accepted(
				MockFullScenarios.RegionalMap(),
				"Traveling to the frontier region."));

		Gateway.RegisterHandler(
			EnterCombat,
			_ => MockCommandResult.Accepted(
				MockFullScenarios.Combat(),
				"An ambush! Roll for initiative."));

		Gateway.RegisterHandler(
			FinishCombat,
			_ => MockCommandResult.Accepted(
				MockFullScenarios.Exploration(),
				"The raiders are defeated."));

		Gateway.RegisterHandler(OpenInventory, _ => MockCommandResult.Accepted(
			Gateway.CurrentSnapshot with
			{
				ActiveModal = MockModalScenarios.Inventory(),
			},
			"Inventory opened."));

		Gateway.RegisterHandler(CloseModal, _ => MockCommandResult.Accepted(
			Gateway.CurrentSnapshot with { ActiveModal = null },
			"Inventory closed."));

		// Latency/busy and recoverable-error placeholders (M5f), both
		// step-based rather than real-time: a wall-clock delay would make
		// this nondeterministic, which §10.3 explicitly rules out. The
		// attempt counters are plain captured locals — each is read only
		// by the one handler closure it belongs to, so a field would add
		// nothing.
		int slowCommandAttempts = 0;
		Gateway.RegisterHandler(SlowCommand, _ =>
		{
			slowCommandAttempts++;
			return slowCommandAttempts < 2
				? MockCommandResult.Busy("Still loading...")
				: MockCommandResult.Accepted(
					Gateway.CurrentSnapshot,
					"Loaded.");
		});

		int riskyActionAttempts = 0;
		Gateway.RegisterHandler(RiskyAction, _ =>
		{
			riskyActionAttempts++;
			return riskyActionAttempts < 2
				? MockCommandResult.Error("Connection lost. Try again.")
				: MockCommandResult.Accepted(
					Gateway.CurrentSnapshot,
					"Action succeeded on retry.");
		});
	}
}
