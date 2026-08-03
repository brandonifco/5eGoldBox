using System;
using System.Collections.Generic;

// The collision-free letter-then-digit hotkey assignment algorithm three
// separate sites (RealGameSession.AssignHotkey,
// ShellInteractionController.RealCombat's AssignSpellHotkey,
// ShellInteractionController.RealSession's AssignAreaHotkey) used to
// duplicate verbatim — Godot's ShowCommands throws on a duplicate
// ShortcutKey within one call, so any source of arbitrary, unhandpicked
// command labels needs this rather than always taking the first letter.
// Has zero Godot dependency, matching RealGameSession's own convention, so
// it belongs beside the other dependency-free models rather than in
// ui/controllers with the Godot-referencing call sites.
internal static class HotkeyAssigner
{
	internal static string Assign(
		IEnumerable<char> candidateLetters,
		HashSet<char> usedHotkeys,
		string subjectLabel)
	{
		foreach (char candidate in candidateLetters)
		{
			if (usedHotkeys.Add(candidate))
			{
				return candidate.ToString();
			}
		}

		for (char digit = '1'; digit <= '9'; digit++)
		{
			if (usedHotkeys.Add(digit))
			{
				return digit.ToString();
			}
		}

		throw new InvalidOperationException(
			$"Could not assign a hotkey for '{subjectLabel}'; every " +
				"letter and digit is already taken in this command set.");
	}
}
