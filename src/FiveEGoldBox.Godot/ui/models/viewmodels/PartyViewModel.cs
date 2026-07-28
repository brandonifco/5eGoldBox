using System.Collections.Generic;

internal sealed record PartyViewModel(
	IReadOnlyList<PartyMemberViewModel> Members,
	string? AggregateStatusText = null);
