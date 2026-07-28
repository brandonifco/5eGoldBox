internal sealed record CommandViewModel(
	string CommandId,
	// Plain text, deliberately not pre-formatted BBCode like the existing
	// Godot-coupled CommandDefinition.FormattedLabel — bolding the hotkey
	// letter is a presentation concern the controller layer applies when
	// it turns this into an actual scene node, not something a supposedly
	// engine-agnostic model should carry.
	string Label,
	string? Hotkey = null,
	bool Enabled = true,
	bool Visible = true,
	string? TooltipText = null,
	string? ReasonText = null);
