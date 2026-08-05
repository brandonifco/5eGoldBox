using System.Collections.Generic;

internal sealed record CharacterSheetViewModel(
	string Name,
	string RaceClassLevel,
	string HealthText,
	int ArmorClass,
	int SpeedFeet,
	IReadOnlyList<CharacterSheetAbilityViewModel> AbilityScores,
	string EncumbranceText,
	bool IsEncumbered,
	string PurseText);

internal sealed record CharacterSheetAbilityViewModel(
	string Abbreviation,
	int Score,
	int Modifier);
