using System.Collections.Generic;
using System.Linq;

// Real driving content for M9's menu-driven secondary screens — the same
// Mock*Content-vs-Mock*Scenarios split MockRegionalMapContent/
// MockCombatContent already established: this is what the live shell
// actually shows, MockModalScenarios (M5) stays the untouched §10.2
// component-gallery catalog. Split into partials by concern once this
// crossed the governing plan's 250-line review threshold — this file
// keeps M9b's Character/Inventory; Spellbook/Area/Journal (M9c/d) are in
// .Adventure.cs, Options/Save-Load (M9e) are in .System.cs. The roster
// here intentionally matches ShellPartyPreviewController's own (Aric/
// Brenna/Cael/Daria/Edrin/Faelan) rather than MockPartyScenarios' generic
// class-named one, so the Character screen shows the same party the
// sidebar already does.
internal static partial class MockSecondaryScreenContent
{
	private static readonly (string Id, string Name, string Class, string HealthText, string Detail)[]
		PartyMembers =
	{
		("aric", "Aric", "Fighter", "12 / 12",
			"A steady blade, first through every door."),
		("brenna", "Brenna", "Rogue", "10 / 10",
			"Quick hands, quicker exit."),
		("cael", "Cael", "Cleric", "9 / 9",
			"Keeps the party standing when the fighting starts."),
		("daria", "Daria", "Wizard", "11 / 11",
			"Reads every room twice before trusting it once."),
		("edrin", "Edrin", "Barbarian", "8 / 8",
			"Reserve muscle for when the front line needs relief."),
		("faelan", "Faelan", "Ranger", "7 / 7",
			"Watches the tree line so no one else has to."),
	};

	private static readonly (string Id, string Label)[] InventoryItems =
	{
		("longsword", "Longsword"),
		("shortbow", "Shortbow (Rogue)"),
		("healing-potion", "Potion of Healing x2"),
		("torch", "Torch x3"),
		("rations", "Trail Rations x6"),
		("rope", "Rope, 50ft"),
	};

	public static ModalViewModel Character(string? selectedMemberId = null)
	{
		string activeId = selectedMemberId ?? PartyMembers[0].Id;

		return new ModalViewModel(
			"Character",
			DescribeMember(activeId),
			PartyMembers
				.Select(member => new CommandViewModel(
					member.Id,
					$"{member.Name} ({member.Class})"))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") },
			SelectedRowId: activeId);
	}

	// Selecting a different party member updates only the body text
	// (ShellInteractionController.SecondaryScreens.cs' onRowFocused calls
	// this via IShellModalScreen.UpdateBody) rather than tearing down and
	// reopening the whole screen — the same "lighter update on focus
	// change" precedent RegionalMapView.SetSelectedLocation set at M7f.
	public static string DescribeMember(string memberId)
	{
		(string Id, string Name, string Class, string HealthText, string Detail) member =
			PartyMembers.First(candidate => candidate.Id == memberId);

		return $"{member.Name} — {member.Class}, {member.HealthText} HP. " +
			$"{member.Detail} Full character sheets are not connected " +
			"to the real engine yet.";
	}

	public static ModalViewModel Inventory()
	{
		return new ModalViewModel(
			"Inventory",
			"Shared party equipment. Equipping and using items is not " +
				"connected to the real engine yet.",
			InventoryItems
				.Select(item => new CommandViewModel(item.Id, item.Label))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") });
	}
}
