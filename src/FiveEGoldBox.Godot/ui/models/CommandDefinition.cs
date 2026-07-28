using System;
using Godot;

internal readonly record struct CommandDefinition(
	string Name,
	string FormattedLabel,
	Key ShortcutKey,
	Action Handler);

