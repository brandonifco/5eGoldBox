using System.Linq;

// Spellbook, Area Map, and Journal content (M9c/d) — split out of
// MockSecondaryScreenContent.cs (see that file's header) once the
// combined class crossed the governing plan's 250-line review threshold.
internal static partial class MockSecondaryScreenContent
{
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
