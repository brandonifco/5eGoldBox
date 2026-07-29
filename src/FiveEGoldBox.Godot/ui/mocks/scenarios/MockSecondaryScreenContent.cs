using System.Collections.Generic;
using System.Linq;

// Real driving content for M9's menu-driven secondary screens — the same
// Mock*Content-vs-Mock*Scenarios split MockRegionalMapContent/
// MockCombatContent already established: this is what the live shell
// actually shows, MockModalScenarios (M5) stays the untouched §10.2
// component-gallery catalog. The roster here intentionally matches
// ShellPartyPreviewController's own (Aric/Brenna/Cael/Daria/Edrin/Faelan)
// rather than MockPartyScenarios' generic class-named one, so the
// Character screen shows the same party the sidebar already does.
internal static class MockSecondaryScreenContent
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

	// Names and mechanics match the campaign ruleset's actual committed
	// six (CampaignRulesetContent.Spells.cs) rather than inventing a
	// separate UI-only spell list — this shell describes real content,
	// even though casting itself isn't connected yet.
	private static readonly (string Id, string Name, string Summary)[] Spells =
	{
		("fire-bolt", "Fire Bolt",
			"Cantrip. 120 ft. A mote of fire streaks out — roll to hit."),
		("sacred-flame", "Sacred Flame",
			"Cantrip. 60 ft. Flame sears a target; they roll to resist it."),
		("cure-wounds", "Cure Wounds",
			"1st level. Touch. Mends wounds with a touch."),
		("healing-word", "Healing Word",
			"1st level. Bonus action, 60 ft. A word of power on the run."),
		("magic-missile", "Magic Missile",
			"1st level. 120 ft. Darts of force strike home automatically."),
		("bless", "Bless",
			"1st level. Concentration. Steadies up to three allies' resolve."),
	};

	public static ModalViewModel Spellbook(string? selectedSpellId = null)
	{
		string activeId = selectedSpellId ?? Spells[0].Id;

		return new ModalViewModel(
			"Spellbook",
			DescribeSpell(activeId),
			Spells
				.Select(spell => new CommandViewModel(spell.Id, spell.Name))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") },
			SelectedRowId: activeId);
	}

	public static string DescribeSpell(string spellId)
	{
		(string Id, string Name, string Summary) spell =
			Spells.First(candidate => candidate.Id == spellId);

		return $"{spell.Name} — {spell.Summary} Casting is not connected " +
			"to the real engine yet.";
	}

	public static string ReadySpellMessage(string spellId)
	{
		(string Id, string Name, string Summary) spell =
			Spells.First(candidate => candidate.Id == spellId);

		return $"You ready {spell.Name}. Casting is not connected to the " +
			"real engine yet.";
	}

	public static ModalViewModel AreaMap()
	{
		return new ModalViewModel(
			"Area",
			"Frontier Outpost — a fortified waypoint on the region's " +
				"edge, watching the road toward the Ruined Watchtower. " +
				"Detailed area surveying is not connected to the real " +
				"engine yet.",
			ListItems: null,
			Commands: new[] { new CommandViewModel("close", "Close", "C") });
	}

	// Names the same Frontier Outpost / Ruined Watchtower narrative
	// MockRegionalMapContent (M7a) already established, rather than
	// inventing a separate unrelated journal narrative.
	private static readonly (string Id, string Label, string Detail, bool Complete)[]
		JournalEntries =
	{
		("investigate-watchtower", "Investigate the Ruined Watchtower",
			"The outpost's quartermaster reports strange signal fires " +
				"from the old watchtower down the road. Active.",
			false),
		("report-outpost", "Report to the Frontier Outpost",
			"Arrived and spoke with the quartermaster. Complete.",
			true),
	};

	public static ModalViewModel Journal(string? selectedEntryId = null)
	{
		string activeId = selectedEntryId ?? JournalEntries[0].Id;

		return new ModalViewModel(
			"Journal",
			DescribeJournalEntry(activeId),
			JournalEntries
				.Select(entry => new CommandViewModel(
					entry.Id,
					entry.Complete ? $"{entry.Label} (Complete)" : entry.Label))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") },
			SelectedRowId: activeId);
	}

	public static string DescribeJournalEntry(string entryId)
	{
		(string Id, string Label, string Detail, bool Complete) entry =
			JournalEntries.First(candidate => candidate.Id == entryId);

		return entry.Detail;
	}
}
