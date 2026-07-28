using System.Collections.Generic;
using System.Linq;

// One method per §10.2 "Commands" minimum case.
internal static class MockCommandScenarios
{
	public static CommandSetViewModel ThreeCommands()
	{
		return CommandSet(Commands(3));
	}

	public static CommandSetViewModel FiveCommands()
	{
		return CommandSet(Commands(5));
	}

	public static CommandSetViewModel SevenCommands()
	{
		return CommandSet(Commands(7));
	}

	public static CommandSetViewModel TenCommands()
	{
		return CommandSet(Commands(10));
	}

	public static CommandSetViewModel DisabledCommand()
	{
		List<CommandViewModel> commands = Commands(4);
		commands[1] = commands[1] with
		{
			Enabled = false,
			ReasonText = "Not enough movement remaining.",
		};

		return CommandSet(commands);
	}

	public static CommandSetViewModel HiddenCommand()
	{
		List<CommandViewModel> commands = Commands(4);
		commands[2] = commands[2] with { Visible = false };

		return CommandSet(commands);
	}

	public static CommandSetViewModel LongLabel()
	{
		List<CommandViewModel> commands = Commands(3);
		commands[0] = commands[0] with
		{
			Label = "Search the room thoroughly for hidden objects",
		};

		return CommandSet(commands);
	}

	// Not a renderable scenario — a deliberately invalid CommandSetViewModel
	// for exercising MockCommandSetValidator.ValidateUniqueHotkeys, which
	// must throw when given it. See the M5c milestone note for how this
	// was actually verified (a scratch harness, same technique as M5b).
	public static CommandSetViewModel DuplicateHotkeyInvalidCommandSet()
	{
		return CommandSet(new List<CommandViewModel>
		{
			new("move", "Move", "M"),
			new("map", "Map", "M"),
		});
	}

	private static List<CommandViewModel> Commands(int count)
	{
		string[] names = { "Move", "View", "Cast", "Area", "Encamp", "Search", "Look", "Rest", "Trade", "Journal" };
		string[] hotkeys = { "M", "V", "C", "A", "E", "S", "L", "R", "T", "J" };

		return Enumerable.Range(0, count)
			.Select(index => new CommandViewModel(
				names[index].ToLowerInvariant(),
				names[index],
				hotkeys[index]))
			.ToList();
	}

	private static CommandSetViewModel CommandSet(IReadOnlyList<CommandViewModel> commands)
	{
		return new CommandSetViewModel(commands, ShellInteractionContext.CommandMenu);
	}
}
