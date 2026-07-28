using System.Collections.Generic;

internal sealed record MessageLogViewModel(
	IReadOnlyList<MessageLogEntryViewModel> Entries);
