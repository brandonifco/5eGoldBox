using System.Collections.Generic;
using System.Linq;

// One method per §10.2 "Messages" minimum case.
internal static class MockMessageScenarios
{
	public static MessageLogViewModel Empty()
	{
		return new MessageLogViewModel(System.Array.Empty<MessageLogEntryViewModel>());
	}

	public static MessageLogViewModel OneLine()
	{
		return new MessageLogViewModel(new[]
		{
			Entry(0, "You enter a quiet stone corridor."),
		});
	}

	public static MessageLogViewModel ManyLines()
	{
		return new MessageLogViewModel(Enumerable.Range(0, 12)
			.Select(index => Entry(index, $"The party advances. (line {index + 1})"))
			.ToList());
	}

	public static MessageLogViewModel LongWrappedLine()
	{
		return new MessageLogViewModel(new[]
		{
			Entry(
				0,
				"The ancient door groans open on rusted hinges, revealing " +
					"a long-forgotten passage lined with faded murals " +
					"depicting a battle no living scholar could name."),
		});
	}

	public static MessageLogViewModel EmphasizedWarning()
	{
		return new MessageLogViewModel(new[]
		{
			Entry(0, "A cold draft signals something is watching."),
			Entry(1, "Warning: the floor ahead looks unstable.", emphasized: true),
		});
	}

	public static MessageLogViewModel CombatResultBurst()
	{
		return new MessageLogViewModel(new[]
		{
			Entry(0, "Fighter attacks the raider: hit for 7 damage.", category: "combat"),
			Entry(1, "Raider attacks Fighter: miss.", category: "combat"),
			Entry(2, "Rogue attacks the raider: critical hit for 14 damage!", category: "combat", emphasized: true),
			Entry(3, "The raider is defeated.", category: "combat", emphasized: true),
		});
	}

	private static MessageLogEntryViewModel Entry(
		int sequence,
		string text,
		string? category = null,
		bool emphasized = false)
	{
		return new MessageLogEntryViewModel(text, sequence, category, emphasized);
	}
}
