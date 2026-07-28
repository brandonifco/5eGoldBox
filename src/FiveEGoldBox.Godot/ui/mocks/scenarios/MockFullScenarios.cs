// Composed, full ShellViewModel scenarios — one per presentation mode —
// for MockScriptedSession to transition among. M5c's factories return
// per-family fragments (a PartyViewModel, a CommandSetViewModel); these
// assemble a few of them into a complete shell state, which is what a
// scripted transition actually needs to hand the gateway.
internal static class MockFullScenarios
{
	public static ShellViewModel Exploration()
	{
		return new ShellViewModel(
			PresentationMode.Exploration,
			Immersive: false,
			Header: new HeaderViewModel("Ruined Watchtower", "Exploration"),
			Party: MockPartyScenarios.SixMembers(),
			Messages: MockMessageScenarios.OneLine(),
			ActiveCommandSet: MockCommandScenarios.SevenCommands());
	}

	public static ShellViewModel RegionalMap()
	{
		return new ShellViewModel(
			PresentationMode.RegionalMap,
			Immersive: false,
			Header: new HeaderViewModel("Frontier Region", "Regional Travel"),
			Party: MockPartyScenarios.SixMembers(),
			Messages: MockMessageScenarios.OneLine(),
			ActiveCommandSet: MockCommandScenarios.FiveCommands());
	}

	public static ShellViewModel Combat()
	{
		return new ShellViewModel(
			PresentationMode.Combat,
			Immersive: false,
			Header: new HeaderViewModel("Ruined Watchtower", "Combat"),
			Party: MockPartyScenarios.MixedHealth(),
			Messages: MockMessageScenarios.CombatResultBurst(),
			ActiveCommandSet: MockCommandScenarios.ThreeCommands());
	}
}
