// The kinds of choice a player makes while building one character. The
// actual sequence is not this order verbatim -- Subrace, Subclass and Spells
// each only appear when the character's own earlier choices call for them,
// which is why CharacterCreationSession recomputes its step plan rather than
// walking a fixed list.
internal enum CharacterCreationStep
{
	Race,
	Subrace,
	Class,
	Subclass,
	Background,
	AbilityScores,
	Skills,
	Spells,
	Name,
	Review
}
