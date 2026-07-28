// Kind is a plain string (e.g. "move-range", "valid-target"), not an
// enum — tactical combat's real highlight vocabulary is still M8's job to
// discover; locking one in now from zero real screens would be exactly
// the kind of guess this milestone's own guidance warns against.
internal sealed record CombatHighlightViewModel(
	int GridX,
	int GridY,
	string Kind);
