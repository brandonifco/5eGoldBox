using System;
using System.Collections.Generic;

// Mirrors ShellCommandBarController.ValidateUniqueShortcuts (M4e), which
// guards the older Godot-coupled CommandDefinition path. CommandSetViewModel
// is a separate, not-yet-wired contract (M5a) with its own string Hotkey,
// so it needs its own check rather than reusing that one directly.
internal static class MockCommandSetValidator
{
	public static void ValidateUniqueHotkeys(CommandSetViewModel commandSet)
	{
		HashSet<string> seenHotkeys = new();

		foreach (CommandViewModel command in commandSet.Commands)
		{
			if (command.Hotkey is null)
			{
				continue;
			}

			if (!seenHotkeys.Add(command.Hotkey))
			{
				throw new ArgumentException(
					$"Command '{command.CommandId}' reuses hotkey " +
						$"'{command.Hotkey}' within the same active " +
						"command set.",
					nameof(commandSet));
			}
		}
	}
}
