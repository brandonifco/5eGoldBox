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

	public static readonly string[] ScriptOrder =
	{
		Travel,
		EnterCombat,
		FinishCombat,
		OpenInventory,
		CloseModal,
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
	}
}
