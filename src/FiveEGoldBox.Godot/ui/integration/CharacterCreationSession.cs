using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Characters;
using FiveEGoldBox.Application.Parties;
using FiveEGoldBox.Core.Characters;
using FiveEGoldBox.Core.Definitions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Validation;

// The wizard behind "create your own party" -- one player-driven pass over
// CharacterCreationRules, the same way RealGameSession wraps SessionView and
// RealCombatSession wraps CombatOperations. Deliberately Godot-free, so it
// can be driven and asserted outside the engine entirely (see this file's
// note in CLAUDE.md about the throwaway-console technique).
//
// Reads its choices from CharacterCreationRules.DescribeOptions and hands
// the finished party back through CharacterCreationRules.CreateParty, so
// every rule this enforces locally is a convenience for the player rather
// than the authority -- the engine re-validates everything on the way in,
// and a draft this lets through that the engine rejects surfaces as a real
// error rather than a silently broken character.
//
// Scoped to the standard array. Point buy is equally supported by the
// backend, but it needs a running-budget control with increment/decrement
// rather than the pick-from-a-list shape every other step here uses, so it
// is deliberately a later increment rather than a half-built third option.
internal sealed partial class CharacterCreationSession
{
	// Descending, which is the order they are offered for assignment --
	// deciding where the best score goes first is the choice that matters
	// most, and StandardArrayRules.Scores is already in this order.
	private static readonly IReadOnlyList<int> StandardArrayScores =
		StandardArrayRules.Scores.OrderByDescending(score => score).ToArray();

	// Starting gear is not modelled as content -- the ruleset has no
	// per-class starting-equipment packages, and every roster character's
	// gear is authored by hand. Rather than invent a weapon-choice step
	// over a weapon list that also contains monster-only weapons with no
	// flag distinguishing them, each class gets the same weapon its own
	// roster counterpart carries. Real starting-equipment content is a
	// separate, larger authoring job.
	private static readonly IReadOnlyDictionary<string, string> DefaultWeaponByClass =
		new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["class.fighter"] = "weapon.longsword",
			["class.barbarian"] = "weapon.greataxe",
			["class.cleric"] = "weapon.mace",
			["class.wizard"] = "weapon.dagger",
			["class.rogue"] = "weapon.dagger",
			["class.ranger"] = "weapon.longbow"
		};

	private const int StartingArrowQuantity = 20;

	private readonly string _rulesetId;
	private readonly int _partySize;
	private readonly CharacterCreationOptions _options;
	private readonly List<CharacterCreationEntry> _finished = new();

	private int _stepIndex;
	private int _abilityAssignmentIndex;

	private string? _raceId;
	private string? _subraceId;
	private string? _classId;
	private string? _subclassId;
	private string? _backgroundId;
	private readonly Dictionary<Ability, int> _abilityScores = new();
	private readonly List<string> _skillIds = new();
	private readonly List<string> _spellIds = new();
	private string _name = string.Empty;

	internal CharacterCreationSession(string rulesetId, int partySize)
	{
		if (partySize <= 0)
		{
			throw new ArgumentOutOfRangeException(
				nameof(partySize),
				partySize,
				"A party requires at least one character.");
		}

		_rulesetId = rulesetId;
		_partySize = partySize;
		_options = CharacterCreationRules.DescribeOptions(rulesetId);
	}

	/// True once every character has been confirmed and BuildParty may run.
	internal bool IsComplete => _finished.Count == _partySize;

	/// Which character of the party is being built right now, 1-based.
	internal int CharacterNumber => _finished.Count + 1;

	internal int PartySize => _partySize;

	internal CharacterCreationStepView Describe()
	{
		CharacterCreationStep step = CurrentStep();

		return step switch
		{
			CharacterCreationStep.Race => DescribeRace(),
			CharacterCreationStep.Subrace => DescribeSubrace(),
			CharacterCreationStep.Class => DescribeClass(),
			CharacterCreationStep.Subclass => DescribeSubclass(),
			CharacterCreationStep.Background => DescribeBackground(),
			CharacterCreationStep.AbilityScores => DescribeAbilityScores(),
			CharacterCreationStep.Skills => DescribeSkills(),
			CharacterCreationStep.Spells => DescribeSpells(),
			CharacterCreationStep.Name => DescribeName(),
			CharacterCreationStep.Review => DescribeReview(),
			_ => throw new InvalidOperationException(
				$"Unhandled character-creation step '{step}'.")
		};
	}

	/// Picks (or, on a multi-select step, toggles) one option.
	internal void SelectOption(string optionId)
	{
		ArgumentNullException.ThrowIfNull(optionId);

		switch (CurrentStep())
		{
			case CharacterCreationStep.Race:
				SelectRace(optionId);
				break;

			case CharacterCreationStep.Subrace:
				_subraceId = optionId;
				break;

			case CharacterCreationStep.Class:
				SelectClass(optionId);
				break;

			case CharacterCreationStep.Subclass:
				_subclassChosenAsNone = optionId == NoSubclassOptionId;
				_subclassId = _subclassChosenAsNone ? null : optionId;
				break;

			case CharacterCreationStep.Background:
				_backgroundId = optionId;
				break;

			case CharacterCreationStep.AbilityScores:
				AssignAbilityScore(optionId);
				break;

			case CharacterCreationStep.Skills:
				Toggle(_skillIds, optionId);
				break;

			case CharacterCreationStep.Spells:
				Toggle(_spellIds, optionId);
				break;
		}
	}

	internal void SetName(string name)
	{
		_name = name ?? string.Empty;
	}

	internal bool CanAdvance() => Describe().CanAdvance;

	/// Moves to the next step, or -- from Review -- confirms the character
	/// and starts the next one. Returns false with a reason when the current
	/// step is not satisfied, or when the engine rejects the finished draft.
	internal bool TryAdvance(out string? error)
	{
		error = null;

		CharacterCreationStepView view = Describe();

		if (!view.CanAdvance)
		{
			error = view.BlockedReason;
			return false;
		}

		if (view.Step != CharacterCreationStep.Review)
		{
			_stepIndex++;
			return true;
		}

		CharacterDraft draft = BuildDraft();
		ValidationResult validation = CharacterCreationRules.Validate(
			draft,
			_rulesetId);

		if (!validation.IsValid)
		{
			error = string.Join(
				" ",
				validation.Issues
					.Where(issue => issue.Severity == ValidationSeverity.Error)
					.Select(issue => issue.Message));

			return false;
		}

		_finished.Add(new CharacterCreationEntry
		{
			PartyMemberId = $"party-member.custom.{_finished.Count + 1}",
			Draft = draft
		});

		ResetForNextCharacter();
		return true;
	}

	/// Steps back one choice. Within ability assignment that means undoing
	/// the last score placed rather than leaving the step, since each score
	/// is its own decision.
	internal void Back()
	{
		if (CurrentStep() == CharacterCreationStep.AbilityScores
			&& _abilityAssignmentIndex > 0)
		{
			_abilityAssignmentIndex--;
			Ability assigned = AbilityAssignedAt(_abilityAssignmentIndex);
			_abilityScores.Remove(assigned);
			return;
		}

		if (_stepIndex > 0)
		{
			_stepIndex--;
		}
	}

	/// True when Back would leave this character entirely -- the caller uses
	/// it to decide whether "Back" returns to the previous character's
	/// summary or out of the wizard altogether.
	internal bool IsAtFirstStep()
	{
		return _stepIndex == 0
			&& (CurrentStep() != CharacterCreationStep.AbilityScores
				|| _abilityAssignmentIndex == 0);
	}

	internal PartyState BuildParty(string partyId)
	{
		if (!IsComplete)
		{
			throw new InvalidOperationException(
				$"The party is not finished: {_finished.Count} of " +
					$"{_partySize} characters have been created.");
		}

		return CharacterCreationRules.CreateParty(
			partyId,
			_finished,
			_rulesetId);
	}
}
