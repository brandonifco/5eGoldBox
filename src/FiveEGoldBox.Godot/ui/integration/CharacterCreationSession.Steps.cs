using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;

// How each step of the wizard renders itself, and how a selection on it is
// recorded. Split from the state machine in CharacterCreationSession.cs the
// same way ShellInteractionController and ScenarioDefinitionValidator split
// their own concerns across partials.
internal sealed partial class CharacterCreationSession
{
	internal const string NoSubclassOptionId = "subclass.none";

	/// The steps this character actually needs, in order. Recomputed rather
	/// than fixed, because three of them exist only when earlier choices
	/// call for them: a race without subraces has nothing to pick, a class
	/// without subclasses likewise, and a non-caster prepares no spells.
	private IReadOnlyList<CharacterCreationStep> StepPlan()
	{
		List<CharacterCreationStep> plan =
		[
			CharacterCreationStep.Race
		];

		if (SelectedRace()?.Subraces.Count > 0)
		{
			plan.Add(CharacterCreationStep.Subrace);
		}

		plan.Add(CharacterCreationStep.Class);

		if (SelectedClass()?.Subclasses.Count > 0)
		{
			plan.Add(CharacterCreationStep.Subclass);
		}

		plan.Add(CharacterCreationStep.Background);
		plan.Add(CharacterCreationStep.AbilityScores);
		plan.Add(CharacterCreationStep.Skills);

		if (SelectedClass()?.SpellcastingAbility is not null)
		{
			plan.Add(CharacterCreationStep.Spells);
		}

		plan.Add(CharacterCreationStep.Name);
		plan.Add(CharacterCreationStep.Review);

		return plan;
	}

	private CharacterCreationStep CurrentStep()
	{
		IReadOnlyList<CharacterCreationStep> plan = StepPlan();

		// Changing an earlier choice can shorten the plan underneath a
		// later index (picking a subrace-less race after a subraced one),
		// so the index is clamped rather than trusted.
		_stepIndex = Math.Clamp(_stepIndex, 0, plan.Count - 1);

		return plan[_stepIndex];
	}

	private string Breadcrumb(string stepName)
	{
		IReadOnlyList<CharacterCreationStep> plan = StepPlan();

		return $"Character {CharacterNumber} of {_partySize}  •  " +
			$"Step {_stepIndex + 1} of {plan.Count}  •  {stepName}";
	}

	private CharacterCreationStepView DescribeRace()
	{
		return SingleChoice(
			CharacterCreationStep.Race,
			"Choose a Race",
			"Race",
			"A race sets your size, speed and the ability scores it raises.",
			_options.Races.Select(race => new CommandViewModel(
				race.Id,
				race.Name,
				TooltipText: DescribeRaceTraits(race))),
			_raceId,
			"Choose a race to continue.");
	}

	private CharacterCreationStepView DescribeSubrace()
	{
		RaceDefinition race = SelectedRace()!;

		return SingleChoice(
			CharacterCreationStep.Subrace,
			$"Choose a {race.Name} Subrace",
			"Subrace",
			$"Every {race.Name} belongs to one of these.",
			race.Subraces.Select(subrace => new CommandViewModel(
				subrace.Id,
				subrace.Name,
				TooltipText: DescribeIncreases(subrace.AbilityScoreIncreases))),
			_subraceId,
			"Choose a subrace to continue.");
	}

	private CharacterCreationStepView DescribeClass()
	{
		return SingleChoice(
			CharacterCreationStep.Class,
			"Choose a Class",
			"Class",
			"Your class decides your hit die, what you are proficient with, "
				+ "and whether you cast spells.",
			_options.Classes.Select(characterClass => new CommandViewModel(
				characterClass.Id,
				characterClass.Name,
				TooltipText: DescribeClassSummary(characterClass))),
			_classId,
			"Choose a class to continue.");
	}

	private CharacterCreationStepView DescribeSubclass()
	{
		ClassDefinition characterClass = SelectedClass()!;

		List<CommandViewModel> options = characterClass.Subclasses
			.Select(subclass => new CommandViewModel(
				subclass.Id,
				subclass.Name))
			.ToList();

		// A subclass is genuinely optional at level 1 -- real 5e picks one
		// above level 1 for three of these four classes, and this engine has
		// no levelling at all, so "not yet" has to be offerable rather than
		// forced. Subclasses currently carry identity only, no mechanics.
		options.Add(new CommandViewModel(
			NoSubclassOptionId,
			"Decide later",
			TooltipText: "Subclasses are a matter of identity for now; "
				+ "none of them changes your character's mechanics yet."));

		return SingleChoice(
			CharacterCreationStep.Subclass,
			$"Choose a {characterClass.Name} Subclass",
			"Subclass",
			"Optional at this level.",
			options,
			_subclassId ?? (_subclassChosenAsNone ? NoSubclassOptionId : null),
			"Choose a subclass, or decide later, to continue.");
	}

	private CharacterCreationStepView DescribeBackground()
	{
		return SingleChoice(
			CharacterCreationStep.Background,
			"Choose a Background",
			"Background",
			"A background grants skill proficiencies on top of the ones your "
				+ "class lets you choose.",
			_options.Backgrounds.Select(background => new CommandViewModel(
				background.Id,
				background.Name,
				TooltipText: DescribeSkillList(background.SkillProficiencies))),
			_backgroundId,
			"Choose a background to continue.");
	}

	private CharacterCreationStepView DescribeAbilityScores()
	{
		bool finished = _abilityAssignmentIndex >= StandardArrayScores.Count;

		string assignedSoFar = _abilityScores.Count == 0
			? "Nothing assigned yet."
			: string.Join(
				"   ",
				Enum.GetValues<Ability>()
					.Where(_abilityScores.ContainsKey)
					.Select(ability =>
						$"{Abbreviate(ability)} {_abilityScores[ability]}"));

		string body = finished
			? $"All six assigned.   {assignedSoFar}"
			: $"Assign {StandardArrayScores[_abilityAssignmentIndex]} to "
				+ $"which ability?\n{assignedSoFar}";

		IReadOnlyList<CommandViewModel> options = finished
			? Array.Empty<CommandViewModel>()
			: Enum.GetValues<Ability>()
				.Where(ability => !_abilityScores.ContainsKey(ability))
				.Select(ability => new CommandViewModel(
					ability.ToString(),
					ability.ToString(),
					TooltipText: DescribeRacialBonusFor(ability)))
				.ToArray();

		return new CharacterCreationStepView(
			CharacterCreationStep.AbilityScores,
			"Assign Ability Scores",
			Breadcrumb("Abilities"),
			body,
			options,
			new HashSet<string>(StringComparer.Ordinal),
			IsMultiSelect: false,
			IsTextEntry: false,
			TextValue: string.Empty,
			CanAdvance: finished,
			BlockedReason: finished
				? null
				: "Assign all six scores to continue.");
	}

	private CharacterCreationStepView DescribeSkills()
	{
		ClassDefinition characterClass = SelectedClass()!;
		int required = characterClass.NumberOfSkillChoices;

		IReadOnlyList<CommandViewModel> options = characterClass.SkillChoices
			.Select(skillId => new CommandViewModel(
				skillId,
				SkillName(skillId)))
			.ToArray();

		bool satisfied = _skillIds.Count == required;

		string granted = DescribeSkillList(
			SelectedBackground()?.SkillProficiencies
				?? Array.Empty<string>());

		string body =
			$"Choose {required} of {options.Count}. "
				+ $"Chosen: {_skillIds.Count}."
			+ (granted.Length == 0
				? string.Empty
				: $"\nYour background already grants {granted}.");

		return new CharacterCreationStepView(
			CharacterCreationStep.Skills,
			"Choose Skills",
			Breadcrumb("Skills"),
			body,
			options,
			new HashSet<string>(_skillIds, StringComparer.Ordinal),
			IsMultiSelect: true,
			IsTextEntry: false,
			TextValue: string.Empty,
			CanAdvance: satisfied,
			BlockedReason: satisfied
				? null
				: $"Choose exactly {required} skill(s) to continue.");
	}

	private CharacterCreationStepView DescribeSpells()
	{
		ClassDefinition characterClass = SelectedClass()!;

		IReadOnlyList<CommandViewModel> options = _options.Spells
			.Select(spell => new CommandViewModel(
				spell.Id,
				spell.Name))
			.ToArray();

		return new CharacterCreationStepView(
			CharacterCreationStep.Spells,
			"Prepare Spells",
			Breadcrumb("Spells"),
			$"{characterClass.Name}s cast using "
				+ $"{characterClass.SpellcastingAbility}. "
				+ $"Prepared: {_spellIds.Count}.",
			options,
			new HashSet<string>(_spellIds, StringComparer.Ordinal),
			IsMultiSelect: true,
			IsTextEntry: false,
			TextValue: string.Empty,
			// The engine enforces no prepared-spell limit at level 1, so
			// neither does this -- including preparing none at all.
			CanAdvance: true,
			BlockedReason: null);
	}

	private CharacterCreationStepView DescribeName()
	{
		bool named = !string.IsNullOrWhiteSpace(_name);

		return new CharacterCreationStepView(
			CharacterCreationStep.Name,
			"Name Your Character",
			Breadcrumb("Name"),
			"What are they called?",
			Array.Empty<CommandViewModel>(),
			new HashSet<string>(StringComparer.Ordinal),
			IsMultiSelect: false,
			IsTextEntry: true,
			TextValue: _name,
			CanAdvance: named,
			BlockedReason: named ? null : "Enter a name to continue.");
	}

	private CharacterCreationStepView DescribeReview()
	{
		return new CharacterCreationStepView(
			CharacterCreationStep.Review,
			$"{_name} — Review",
			Breadcrumb("Review"),
			BuildReviewText(),
			Array.Empty<CommandViewModel>(),
			new HashSet<string>(StringComparer.Ordinal),
			IsMultiSelect: false,
			IsTextEntry: false,
			TextValue: string.Empty,
			CanAdvance: true,
			BlockedReason: null);
	}

	private string BuildReviewText()
	{
		string race = SelectedSubrace()?.Name ?? SelectedRace()?.Name ?? "—";
		string characterClass = SelectedClass()?.Name ?? "—";
		string subclass = SelectedSubclass()?.Name ?? "none yet";

		string scores = string.Join(
			"   ",
			Enum.GetValues<Ability>()
				.Where(_abilityScores.ContainsKey)
				.Select(ability =>
					$"{Abbreviate(ability)} {_abilityScores[ability]}"));

		string spells = _spellIds.Count == 0
			? string.Empty
			: $"\nSpells: {string.Join(", ", _spellIds.Select(SpellName))}";

		return $"{race} {characterClass} (subclass: {subclass})\n"
			+ $"Background: {SelectedBackground()?.Name ?? "—"}\n"
			+ $"Base scores: {scores}\n"
			+ $"Skills: {DescribeSkillList(_skillIds)}"
			+ spells
			+ "\n\nRacial bonuses are added on top of the base scores when "
			+ "the character is built.";
	}

	private CharacterCreationStepView SingleChoice(
		CharacterCreationStep step,
		string title,
		string stepName,
		string body,
		IEnumerable<CommandViewModel> options,
		string? selectedId,
		string blockedReason)
	{
		HashSet<string> selected = new(StringComparer.Ordinal);

		if (selectedId is not null)
		{
			selected.Add(selectedId);
		}

		return new CharacterCreationStepView(
			step,
			title,
			Breadcrumb(stepName),
			body,
			options.ToArray(),
			selected,
			IsMultiSelect: false,
			IsTextEntry: false,
			TextValue: string.Empty,
			CanAdvance: selectedId is not null,
			BlockedReason: selectedId is not null ? null : blockedReason);
	}
}
