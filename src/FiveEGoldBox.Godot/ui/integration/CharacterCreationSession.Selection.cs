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
