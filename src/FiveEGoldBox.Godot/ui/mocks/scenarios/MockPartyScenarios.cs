using System.Collections.Generic;

// One method per §10.2 "Party" minimum case. Deterministic, named,
// UI-local placeholder content — makes no claim about real character
// data or 5e rules.
internal static class MockPartyScenarios
{
	public static PartyViewModel SixMembers()
	{
		return new PartyViewModel(new[]
		{
			Member("fighter", "Fighter", "12/12", 1.0),
			Member("rogue", "Rogue", "9/9", 1.0),
			Member("cleric", "Cleric", "10/10", 1.0),
			Member("wizard", "Wizard", "7/7", 1.0),
			Member("barbarian", "Barbarian", "15/15", 1.0),
			Member("ranger", "Ranger", "11/11", 1.0),
		});
	}

	public static PartyViewModel FewerMembers()
	{
		return new PartyViewModel(new[]
		{
			Member("fighter", "Fighter", "12/12", 1.0),
			Member("cleric", "Cleric", "10/10", 1.0),
		});
	}

	public static PartyViewModel LongNames()
	{
		return new PartyViewModel(new[]
		{
			Member(
				"fighter",
				"Sir Reginald Thistlewood the Unbowed",
				"12/12",
				1.0),
			Member(
				"cleric",
				"High Devotee Wilhelmina of the Silver Dawn",
				"10/10",
				1.0),
		});
	}

	public static PartyViewModel MixedHealth()
	{
		return new PartyViewModel(new[]
		{
			Member("fighter", "Fighter", "12/12", 1.0),
			Member("rogue", "Rogue", "4/9", 0.44),
			Member("cleric", "Cleric", "2/10", 0.2),
			Member("wizard", "Wizard", "0/7", 0.0),
		});
	}

	public static PartyViewModel StatusEffects()
	{
		return new PartyViewModel(new[]
		{
			Member(
				"fighter",
				"Fighter",
				"12/12",
				1.0,
				conditionText: "Blessed"),
			Member(
				"rogue",
				"Rogue",
				"9/9",
				1.0,
				conditionText: "Poisoned"),
			Member(
				"cleric",
				"Cleric",
				"10/10",
				1.0,
				conditionText: "Concentrating"),
		});
	}

	public static PartyViewModel SelectedMember()
	{
		return new PartyViewModel(new[]
		{
			Member("fighter", "Fighter", "12/12", 1.0, selected: true),
			Member("cleric", "Cleric", "10/10", 1.0),
		});
	}

	public static PartyViewModel NoPortrait()
	{
		return new PartyViewModel(new[]
		{
			Member("fighter", "Fighter", "12/12", 1.0, portraitKey: null),
			Member("cleric", "Cleric", "10/10", 1.0, portraitKey: null),
		});
	}

	public static PartyViewModel LongHealthText()
	{
		return new PartyViewModel(new[]
		{
			Member(
				"fighter",
				"Fighter",
				"12 / 12 (+2 temporary, poisoned -1/round)",
				1.0),
		});
	}

	private static PartyMemberViewModel Member(
		string id,
		string displayName,
		string healthText,
		double healthFraction,
		string? conditionText = null,
		string? portraitKey = "portrait-default",
		bool selected = false)
	{
		return new PartyMemberViewModel(
			id,
			displayName,
			healthText,
			healthFraction,
			conditionText,
			portraitKey,
			selected);
	}
}
