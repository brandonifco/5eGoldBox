using Godot;

internal readonly record struct UiResolutionPreset(
	string Id,
	string DisplayName,
	Vector2I Size);
