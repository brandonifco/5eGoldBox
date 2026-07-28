using System;
using Godot;

// Bridges the M5 view-model layer to the existing Godot-coupled
// CommandDefinition/HotkeyCommandButton pipeline (M0-M3), so a controller
// can hold its command set as data and translate it, rather than
// constructing CommandDefinitions by hand. Reusable across M6/M7/M8 —
// not exploration-specific.
internal static class CommandViewModelTranslator
{
	public static CommandDefinition ToCommandDefinition(
		CommandViewModel model,
		Action handler)
	{
		if (model.Hotkey is null)
		{
			throw new ArgumentException(
				$"Command '{model.CommandId}' has no Hotkey; " +
					"CommandDefinition requires one.",
				nameof(model));
		}

		Key shortcutKey = Enum.Parse<Key>(model.Hotkey, ignoreCase: true);

		return new CommandDefinition(
			model.Label,
			FormatLabel(model.Label, model.Hotkey[0]),
			shortcutKey,
			handler);
	}

	private static string FormatLabel(string label, char hotkeyChar)
	{
		int index = label.IndexOf(
			hotkeyChar,
			StringComparison.OrdinalIgnoreCase);

		if (index < 0)
		{
			return label;
		}

		return $"{label[..index]}[b]{label[index]}[/b]{label[(index + 1)..]}";
	}
}
