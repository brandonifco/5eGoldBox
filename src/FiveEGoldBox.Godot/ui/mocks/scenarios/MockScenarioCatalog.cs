using System;
using System.Collections.Generic;

// The named, enumerable index §10.3 requires ("mocks are selected through
// developer tooling, not hidden hard-coded edits"; "mock IDs remain
// UI-local"). One dictionary per family rather than a single
// Dictionary<string, Func<object>> — each family returns a different view
// model, and boxing them behind object would be exactly the "generic
// object" the §7.3 payload guidance separately warns against, applied
// here for the same reason. M5d's picker enumerates these; it doesn't
// need a new index of its own.
internal static class MockScenarioCatalog
{
	public static readonly IReadOnlyDictionary<string, Func<PartyViewModel>> Party =
		new Dictionary<string, Func<PartyViewModel>>
		{
			["six-members"] = MockPartyScenarios.SixMembers,
			["fewer-members"] = MockPartyScenarios.FewerMembers,
			["long-names"] = MockPartyScenarios.LongNames,
			["mixed-health"] = MockPartyScenarios.MixedHealth,
			["status-effects"] = MockPartyScenarios.StatusEffects,
			["selected-member"] = MockPartyScenarios.SelectedMember,
			["no-portrait"] = MockPartyScenarios.NoPortrait,
			["long-health-text"] = MockPartyScenarios.LongHealthText,
		};

	public static readonly IReadOnlyDictionary<string, Func<CommandSetViewModel>> Commands =
		new Dictionary<string, Func<CommandSetViewModel>>
		{
			["three-commands"] = MockCommandScenarios.ThreeCommands,
			["five-commands"] = MockCommandScenarios.FiveCommands,
			["seven-commands"] = MockCommandScenarios.SevenCommands,
			["ten-commands"] = MockCommandScenarios.TenCommands,
			["disabled-command"] = MockCommandScenarios.DisabledCommand,
			["hidden-command"] = MockCommandScenarios.HiddenCommand,
			["long-label"] = MockCommandScenarios.LongLabel,
			["duplicate-hotkey-invalid"] =
				MockCommandScenarios.DuplicateHotkeyInvalidCommandSet,
		};

	public static readonly IReadOnlyDictionary<string, Func<MessageLogViewModel>> Messages =
		new Dictionary<string, Func<MessageLogViewModel>>
		{
			["empty"] = MockMessageScenarios.Empty,
			["one-line"] = MockMessageScenarios.OneLine,
			["many-lines"] = MockMessageScenarios.ManyLines,
			["long-wrapped-line"] = MockMessageScenarios.LongWrappedLine,
			["emphasized-warning"] = MockMessageScenarios.EmphasizedWarning,
			["combat-result-burst"] = MockMessageScenarios.CombatResultBurst,
		};

	public static readonly IReadOnlyDictionary<string, Func<ExplorationViewModel>> Exploration =
		new Dictionary<string, Func<ExplorationViewModel>>
		{
			["building"] = MockExplorationScenarios.Building,
			["town-street"] = MockExplorationScenarios.TownStreet,
			["cavern"] = MockExplorationScenarios.Cavern,
			["dungeon-corridor"] = MockExplorationScenarios.DungeonCorridor,
			["interaction-prompt"] = MockExplorationScenarios.InteractionPrompt,
			["movement-active"] = MockExplorationScenarios.MovementActive,
		};

	public static readonly IReadOnlyDictionary<string, Func<RegionalMapViewModel>> RegionalMap =
		new Dictionary<string, Func<RegionalMapViewModel>>
		{
			["no-nearby-locations"] = MockRegionalMapScenarios.NoNearbyLocations,
			["dense-location-cluster"] = MockRegionalMapScenarios.DenseLocationCluster,
			["selected-location"] = MockRegionalMapScenarios.SelectedLocation,
			["route-preview"] = MockRegionalMapScenarios.RoutePreview,
			["travel-warning"] = MockRegionalMapScenarios.TravelWarning,
		};

	public static readonly IReadOnlyDictionary<string, Func<CombatViewModel>> Combat =
		new Dictionary<string, Func<CombatViewModel>>
		{
			["small-grid"] = MockCombatScenarios.SmallGrid,
			["large-grid"] = MockCombatScenarios.LargeGrid,
			["many-combatants"] = MockCombatScenarios.ManyCombatants,
			["active-actor"] = MockCombatScenarios.ActiveActor,
			["valid-invalid-highlights"] = MockCombatScenarios.ValidInvalidHighlights,
			["target-selection"] = MockCombatScenarios.TargetSelection,
			["completed-encounter-display"] = MockCombatScenarios.CompletedEncounterDisplay,
		};

	public static readonly IReadOnlyDictionary<string, Func<ModalViewModel>> Modals =
		new Dictionary<string, Func<ModalViewModel>>
		{
			["inventory"] = MockModalScenarios.Inventory,
			["spellbook"] = MockModalScenarios.Spellbook,
			["character-view"] = MockModalScenarios.CharacterView,
			["encamp"] = MockModalScenarios.Encamp,
			["journal"] = MockModalScenarios.Journal,
			["shop"] = MockModalScenarios.Shop,
			["inn"] = MockModalScenarios.Inn,
			["temple"] = MockModalScenarios.Temple,
			["save-load"] = MockModalScenarios.SaveLoad,
			["confirmation"] = MockModalScenarios.Confirmation,
		};
}
