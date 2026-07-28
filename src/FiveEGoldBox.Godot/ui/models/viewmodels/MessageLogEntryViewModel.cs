internal sealed record MessageLogEntryViewModel(
	string Text,
	int Sequence,
	string? Category = null,
	bool Emphasized = false);
