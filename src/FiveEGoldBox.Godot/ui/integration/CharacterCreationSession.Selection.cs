using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

// Recording a choice, looking up what was chosen, and turning the result
// into a real CharacterDraft.
internal sealed partial class CharacterCreationSession
{
	// Which ability each of the six standard-array scores went to, in the
	// order they were placed -- Back needs to undo the last placement
	// specifically, which a Dictionary<Ability,int> alone cannot say.
	private readonly List<Ability> _abilityAssignmentOrder = new();

	// "Decide later" is a real answer to the subclass step, and has to be
	// distinguishable from "not answered yet" -- both leave _subclassId
	// null, but only one of them satisfies the step.
	private bool _subclassChosenAsNone;

	private void SelectRace(string raceId)
	{
		if (string.Equals(_raceId, raceId, StringComparison.Ordinal))
		{
			return;
		}

		_raceId = raceId;

		// A subrace belongs to the race it was picked under; keeping it
		// after switching races would silently carry an illegal pairing
		// into validation.
		_subraceId = null;
	}

	private void SelectClass(string classId)
	{
		if (string.Equals(_classId, classId, StringComparison.Ordinal))
		{
			return;
		}

		_classId = classId;

		// Same reasoning as SelectRace, plus skills and spells: a skill
		// list is the class's own, and a non-caster prepares nothing.
		_subclassId = null;
		_subclassChosenAsNone = false;
		_skillIds.Clear();
		_spellIds.Clear();
	}

	// Marks which ability a row activation targeted, without touching
	// _abilityScores yet -- the actual commit (AssignAbilityScore) only
	// happens when TryAdvance ("Next") is called, so browsing/reselecting
	// among the remaining abilities before confirming never burns a
	// standard-array value.
	private void SelectPendingAbility(string abilityName)
	{
		if (_abilityAssignmentIndex >= StandardArrayScores.Count
			|| !Enum.TryParse(abilityName, out Ability ability)
			|| _abilityScores.ContainsKey(ability))
		{
			return;
		}

		_pendingAbilityAssignment = ability;
	}

	private void AssignAbilityScore(string abilityName)
	{
		if (_abilityAssignmentIndex >= StandardArrayScores.Count)
		{
			return;
		}

		if (!Enum.TryParse(abilityName, out Ability ability)
			|| _abilityScores.ContainsKey(ability))
		{
			return;
		}

		_abilityScores[ability] = StandardArrayScores[_abilityAssignmentIndex];

		if (_abilityAssignmentOrder.Count > _abilityAssignmentIndex)
		{
			_abilityAssignmentOrder[_abilityAssignmentIndex] = ability;
		}
		else
		{
			_abilityAssignmentOrder.Add(ability);
		}

		_abilityAssignmentIndex++;
	}

	private Ability AbilityAssignedAt(int index) =>
		_abilityAssignmentOrder[index];

	private static void Toggle(List<string> selected, string optionId)
	{
		if (!selected.Remove(optionId))
		{
			selected.Add(optionId);
		}
	}

	private void ResetForNextCharacter()
	{
		_stepIndex = 0;
		_abilityAssignmentIndex = 0;
		_pendingAbilityAssignment = null;
		_raceId = null;
		_subraceId = null;
		_classId = null;
		_subclassId = null;
		_subclassChosenAsNone = false;
		_backgroundId = null;
		_abilityScores.Clear();
		_abilityAssignmentOrder.Clear();
		_skillIds.Clear();
		_spellIds.Clear();
		_name = string.Empty;
	}

	private CharacterDraft BuildDraft()
	{
		List<InventoryItemDraft> inventory = new();
		List<string> weapons = new();

		if (_classId is not null
			&& DefaultWeaponByClass.TryGetValue(_classId, out string? weaponId))
		{
			weapons.Add(weaponId);

			WeaponDefinition? weapon = _options.Weapons.FirstOrDefault(
				candidate => string.Equals(
					candidate.Id,
					weaponId,
					StringComparison.Ordinal));

			if (weapon?.AmmunitionItemId is not null)
			{
				inventory.Add(new InventoryItemDraft
				{
					ItemId = weapon.AmmunitionItemId,
					Quantity = StartingArrowQuantity
				});
			}
		}

		return new CharacterDraft
		{
			Name = _name.Trim(),
			Level = 1,
			RaceId = _raceId,
			SubraceId = _subraceId,
			ClassId = _classId,
			SubclassId = _subclassId,
			BackgroundId = _backgroundId,
			AbilityScoreGenerationMethod =
				AbilityScoreGenerationMethod.StandardArray,
			BaseAbilityScores = new Dictionary<Ability, int>(_abilityScores),
			SelectedSkillIds = _skillIds.ToArray(),
			PreparedSpellIds = _spellIds.ToArray(),
			EquippedWeaponIds = weapons.ToArray(),
			InventoryItems = inventory.ToArray()
		};
	}

	private RaceDefinition? SelectedRace() =>
		Find(_options.Races, _raceId, race => race.Id);

	private SubraceDefinition? SelectedSubrace() =>
		_subraceId is null
			? null
			: SelectedRace()?.Subraces.FirstOrDefault(
				subrace => string.Equals(
					subrace.Id,
					_subraceId,
					StringComparison.Ordinal));

	private ClassDefinition? SelectedClass() =>
		Find(_options.Classes, _classId, characterClass => characterClass.Id);

	private SubclassDefinition? SelectedSubclass() =>
		_subclassId is null
			? null
			: SelectedClass()?.Subclasses.FirstOrDefault(
				subclass => string.Equals(
					subclass.Id,
					_subclassId,
					StringComparison.Ordinal));

	private BackgroundDefinition? SelectedBackground() =>
		Find(_options.Backgrounds, _backgroundId, background => background.Id);

	private static T? Find<T>(
		IReadOnlyList<T> items,
		string? id,
		Func<T, string> idOf)
		where T : class
	{
		return id is null
			? null
			: items.FirstOrDefault(
				item => string.Equals(idOf(item), id, StringComparison.Ordinal));
	}

	private string SkillName(string skillId)
	{
		return _options.Skills
			.FirstOrDefault(skill => string.Equals(
				skill.Id,
				skillId,
				StringComparison.Ordinal))
			?.Name ?? skillId;
	}

	private string? SkillDescription(string skillId)
	{
		return _options.Skills
			.FirstOrDefault(skill => string.Equals(
				skill.Id,
				skillId,
				StringComparison.Ordinal))
			?.Description;
	}

	// Every step's own preview text is the option's real authored prose
	// first (the same PHB-style descriptions Phase 2 wrote for exactly
	// this purpose, never previously connected to any screen) followed by
	// the mechanical summary that already existed -- so a player who
	// wants the flavor gets it, and the numbers are still right there
	// underneath it, not replaced by it. Prose is genuinely optional
	// content (a spell has none authored yet), so this degrades to just
	// the summary rather than leaving a blank line where the description
	// would go.
	private static string CombineDescription(string? prose, string mechanicalSummary)
	{
		return prose is null ? mechanicalSummary : $"{prose}\n\n{mechanicalSummary}";
	}

	private string SpellName(string spellId)
	{
		return _options.Spells
			.FirstOrDefault(spell => string.Equals(
				spell.Id,
				spellId,
				StringComparison.Ordinal))
			?.Name ?? spellId;
	}

	private string DescribeSkillList(IEnumerable<string> skillIds)
	{
		string[] names = skillIds.Select(SkillName).ToArray();

		return names.Length == 0 ? string.Empty : string.Join(", ", names);
	}

	private static string DescribeIncreases(
		IReadOnlyList<AbilityScoreIncrease> increases)
	{
		return increases.Count == 0
			? "No ability score increases."
			: string.Join(
				", ",
				increases.Select(increase =>
					$"{Abbreviate(increase.Ability)} +{increase.Amount}"));
	}

	private static string DescribeRaceTraits(RaceDefinition race)
	{
		List<string> parts =
		[
			$"Speed {race.BaseSpeedFeet} ft.",
			race.Size.ToString(),
			DescribeIncreases(race.AbilityScoreIncreases)
		];

		foreach (SenseDefinition sense in race.Senses)
		{
			parts.Add($"{sense.Type} {sense.RangeFeet} ft.");
		}

		return string.Join("  •  ", parts);
	}

	private static string DescribeClassSummary(ClassDefinition characterClass)
	{
		string casting = characterClass.SpellcastingAbility is null
			? "No spellcasting."
			: $"Casts using {characterClass.SpellcastingAbility}.";

		return $"Hit die {characterClass.HitDie}.  "
			+ $"Chooses {characterClass.NumberOfSkillChoices} of "
			+ $"{characterClass.SkillChoices.Count} skills.  {casting}";
	}

	// What this ability actually governs at this table -- original,
	// short, and scoped to what this ruleset's own content actually does
	// with it (only Cleric casts on Wisdom and Wizard on Intelligence
	// today; no class here uses Charisma to cast), not a restatement of
	// the Player's Handbook's own text. The skill lists match Skills.
	// Ability in the ruleset exactly, not general 5e lore, so this stays
	// correct if the skill-to-ability mapping is ever re-authored.
	private static string DescribeAbilityUse(Ability ability)
	{
		return ability switch
		{
			Ability.Strength =>
				"Melee attack and damage rolls, Athletics checks, and how "
					+ "much you can carry.",
			Ability.Dexterity =>
				"Armor Class, initiative, ranged and finesse attacks, and "
					+ "Acrobatics, Sleight of Hand and Stealth checks.",
			Ability.Constitution =>
				"How many hit points you have, and resisting poison, "
					+ "disease and exhaustion.",
			Ability.Intelligence =>
				"Arcana, History, Investigation, Nature and Religion "
					+ "checks, and how a Wizard casts spells.",
			Ability.Wisdom =>
				"Animal Handling, Insight, Medicine, Perception and "
					+ "Survival checks, and how a Cleric casts spells.",
			Ability.Charisma =>
				"Deception, Intimidation, Performance and Persuasion "
					+ "checks.",
			_ => ability.ToString()
		};
	}

	// What this ability does, plus class-reliance and racial-bonus notes
	// sharing one line when either or both apply -- each used to be its
	// own paragraph, which cost a blank-line gap per note and pushed the
	// whole thing well past the space actually reserved for it.
	private string CombineAbilityTooltip(Ability ability)
	{
		List<string> notes = new();

		if (DescribeClassRelianceFor(ability) is string classReliance)
		{
			notes.Add(classReliance);
		}

		if (DescribeRacialBonusFor(ability) is string bonus)
		{
			notes.Add(bonus);
		}

		string use = DescribeAbilityUse(ability);

		return notes.Count == 0 ? use : $"{use}\n\n{string.Join(" ", notes)}";
	}

	// The class step always comes before this one (see StepPlan), so the
	// player already has a class by the time this renders -- this answers
	// "which ability should I favor" against their own actual pick rather
	// than dumping every class's PrimaryAbilities, which would bury the
	// one line that's actually relevant to them under five that aren't.
	private string? DescribeClassRelianceFor(Ability ability)
	{
		ClassDefinition? characterClass = SelectedClass();

		if (characterClass is null || !characterClass.PrimaryAbilities.Contains(ability))
		{
			return null;
		}

		return $"Your {characterClass.Name} relies on this ability.";
	}

	/// What the already-chosen race and subrace would add to this ability,
	/// so the player can see where a racial bonus lands before committing a
	/// standard-array score to it.
	private string? DescribeRacialBonusFor(Ability ability)
	{
		int bonus = Sum(SelectedRace()?.AbilityScoreIncreases)
			+ Sum(SelectedSubrace()?.AbilityScoreIncreases);

		return bonus == 0 ? null : $"Your race adds +{bonus} to this.";

		int Sum(IReadOnlyList<AbilityScoreIncrease>? increases) =>
			increases is null
				? 0
				: increases
					.Where(increase => increase.Ability == ability)
					.Sum(increase => increase.Amount);
	}

	private static string Abbreviate(Ability ability)
	{
		return ability switch
		{
			Ability.Strength => "STR",
			Ability.Dexterity => "DEX",
			Ability.Constitution => "CON",
			Ability.Intelligence => "INT",
			Ability.Wisdom => "WIS",
			Ability.Charisma => "CHA",
			_ => ability.ToString()
		};
	}
}
