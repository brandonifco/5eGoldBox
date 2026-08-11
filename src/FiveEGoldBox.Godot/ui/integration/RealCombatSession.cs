using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
// Godot's own CombatView (ui/presentation/CombatView.cs, a Control-
// derived scene, global/no-namespace) collides with the name of
// FiveEGoldBox.Application.Combat's own CombatView (a data record for
// the whole battlefield) — this alias disambiguates rather than fully
// qualifying every reference.
using EncounterView = FiveEGoldBox.Application.Combat.CombatView;

// The real integration seam for combat — the direct sibling of
// RealGameSession, but scoped to exactly one encounter's lifetime rather
// than the whole session. Built on CombatOperations, the same facade
// FiveEGoldBox.Console's ConsoleSessionRunner.RunCombatSession already
// drives end to end; this ports that state machine into per-call methods
// since Godot re-enters on each command instead of blocking in a loop.
//
// Scoped deliberately to single-target actions: Move, Weapon Attack, Spell
// Attack, End Turn. Multi-target spells (Bless's TargetCombinations) are
// proven at the backend but not wired here yet. Death saving throws are
// never a real player decision — CombatOperations resolves them
// automatically the moment a dying combatant's turn comes up — so
// "wiring" one just means narrating the roll DescribeDeathSavingThrow
// already produces below.
internal sealed class RealCombatSession
{
	private ApplicationSessionState _state;

	internal RealCombatSession(ApplicationSessionState state)
	{
		// Runs any pre-player automatic processing (e.g. a surprise round)
		// before the first snapshot is ever described — the same thing
		// Console's RunCombatSession does immediately on entry.
		CombatResolutionResult opening =
			CombatOperations.AdvanceToDecision(state);
		_state = opening.State;

		// Narrates whatever that opening processing actually did (e.g. an
		// enemy who wins initiative acting before the party's first turn)
		// so the combat log's first entries are the true start of the
		// fight, not silently skipped the way they were before the log
		// existed to show them.
		IReadOnlyDictionary<string, string> openingDisplayNames =
			CombatOperations.Query(_state).Combatants
				.ToDictionary(
					combatant => combatant.CombatantId,
					combatant => combatant.DisplayName,
					StringComparer.Ordinal);

		List<string> openingLines = new();

		foreach (CombatStepResult step in opening.AutomaticSteps)
		{
			DescribeStep(step, openingDisplayNames, openingLines);
		}

		OpeningLines = openingLines;
	}

	internal ApplicationSessionState State => _state;

	// Whatever automatic processing ran before the party's first real
	// decision -- empty for the ordinary case (the party simply acts
	// first). Read once, right after construction, to seed the combat log.
	internal IReadOnlyList<string> OpeningLines { get; }

	internal RealCombatSnapshot Describe()
	{
		return Describe(statusMessage: null);
	}

	internal IReadOnlyList<string> SubmitMove(IReadOnlyList<GridPosition> path)
	{
		CombatDecision decision = CombatOperations.Query(_state).Decision;

		CombatResolutionResult result = CombatOperations.Execute(
			_state,
			new CombatMoveIntent
			{
				ExpectedEncounterRevision = decision.EncounterRevision,
				ActorCombatantId = decision.ActiveCombatantId!,
				Path = path,
			});

		return Advance(result);
	}

	internal IReadOnlyList<string> SubmitWeaponAttack(string weaponId, string targetCombatantId)
	{
		CombatDecision decision = CombatOperations.Query(_state).Decision;

		CombatResolutionResult result = CombatOperations.Execute(
			_state,
			new CombatWeaponAttackIntent
			{
				ExpectedEncounterRevision = decision.EncounterRevision,
				ActorCombatantId = decision.ActiveCombatantId!,
				WeaponId = weaponId,
				TargetCombatantId = targetCombatantId,
			});

		return Advance(result);
	}

	internal IReadOnlyList<string> SubmitDisengage()
	{
		CombatDecision decision = CombatOperations.Query(_state).Decision;

		CombatResolutionResult result = CombatOperations.Execute(
			_state,
			new CombatDisengageIntent
			{
				ExpectedEncounterRevision = decision.EncounterRevision,
				ActorCombatantId = decision.ActiveCombatantId!,
			});

		return Advance(result);
	}

	internal IReadOnlyList<string> SubmitEndTurn()
	{
		CombatDecision decision = CombatOperations.Query(_state).Decision;

		CombatResolutionResult result = CombatOperations.Execute(
			_state,
			new CombatEndTurnIntent
			{
				ExpectedEncounterRevision = decision.EncounterRevision,
				ActorCombatantId = decision.ActiveCombatantId!,
			});

		return Advance(result);
	}

	// Single-target only, matching SubmitWeaponAttack — a spell that can
	// name additional targets (Bless) is offered here one target at a
	// time via its own Targets list; TargetCombinations (casting on 2+ at
	// once) is a fast-follow, same scope cut as multi-target attacks.
	internal IReadOnlyList<string> SubmitSpellAttack(string spellId, string targetCombatantId)
	{
		CombatDecision decision = CombatOperations.Query(_state).Decision;

		CombatResolutionResult result = CombatOperations.Execute(
			_state,
			new CombatSpellAttackIntent
			{
				ExpectedEncounterRevision = decision.EncounterRevision,
				ActorCombatantId = decision.ActiveCombatantId!,
				SpellId = spellId,
				TargetCombatantId = targetCombatantId,
				AdditionalTargetCombatantIds = Array.Empty<string>(),
			});

		return Advance(result);
	}

	// Every Execute is immediately followed by AdvanceToDecision — this is
	// what makes enemy AI turns actually run and land the caller back at
	// the next real player decision (or combat completion) in one round
	// trip, matching Console's own loop-back. CombatDecisionState.
	// AutomaticProcessingRequired never leaks past this method, the same
	// invariant Console's ValidatePlayerDecision enforces.
	private IReadOnlyList<string> Advance(CombatResolutionResult submitted)
	{
		CombatResolutionResult settled =
			CombatOperations.AdvanceToDecision(submitted.State);
		_state = settled.State;

		// Combatant names don't change mid-encounter, so one query supplies
		// every step this round-trip describes rather than one per line.
		IReadOnlyDictionary<string, string> displayNames =
			CombatOperations.Query(_state).Combatants
				.ToDictionary(
					combatant => combatant.CombatantId,
					combatant => combatant.DisplayName,
					StringComparer.Ordinal);

		List<string> lines = new();

		if (submitted.PrimaryStep is not null)
		{
			DescribeStep(submitted.PrimaryStep, displayNames, lines);
		}

		foreach (CombatStepResult step in submitted.AutomaticSteps)
		{
			DescribeStep(step, displayNames, lines);
		}

		foreach (CombatStepResult step in settled.AutomaticSteps)
		{
			DescribeStep(step, displayNames, lines);
		}

		if (lines.Count == 0)
		{
			lines.Add("Nothing happened.");
		}

		return lines;
	}

	private RealCombatSnapshot Describe(string? statusMessage)
	{
		EncounterView view = CombatOperations.Query(_state);
		CombatDecision decision = view.Decision;

		// A party member's CombatantId is already its own role identity
		// ("party-member.fighter") with no MonsterId; a monster's
		// CombatantId is a per-placement instance ID ("combatant.mill-
		// rat.first") distinct from the shared type both catalogs below
		// actually key on ("monster.mill-rat") -- MonsterId is that
		// shared type, null for party members, so falling back to
		// CombatantId when it's null is exactly the "party member or
		// monster, whichever this combatant is" resolution both catalogs
		// need.
		IReadOnlyList<CombatantMarkerViewModel> combatants = view.Combatants
			.Select(combatant => new CombatantMarkerViewModel(
				combatant.CombatantId,
				combatant.DisplayName,
				combatant.Position.X,
				combatant.Position.Y,
				Active: combatant.CombatantId == decision.ActiveCombatantId,
				IsAlly: combatant.SideId == view.PartySideId,
				CurrentHitPoints: combatant.Health.HitPoints.CurrentHitPoints,
				MaximumHitPoints: combatant.Health.HitPoints.MaximumHitPoints,
				PortraitResourcePath: CombatantPortraitCatalog.Resolve(
					combatant.MonsterId ?? combatant.CombatantId)))
			.ToArray();

		bool isCompleted = decision.State == CombatDecisionState.CombatCompleted;
		bool isPlayerTurn = decision.State == CombatDecisionState.PlayerDecisionRequired;

		// The turn order strip's own entries -- looked up against the
		// CombatantMarkerViewModels just built above rather than a second
		// resolution pass, since every ID in view.InitiativeOrder already
		// has a matching entry there.
		Dictionary<string, CombatantMarkerViewModel> combatantsById =
			combatants.ToDictionary(
				combatant => combatant.Id,
				StringComparer.Ordinal);

		IReadOnlyList<InitiativeEntryViewModel> initiativeOrder = view
			.InitiativeOrder
			.Where(combatantsById.ContainsKey)
			.Select(id => combatantsById[id])
			.Select(combatant => new InitiativeEntryViewModel(
				combatant.Id,
				combatant.Label,
				combatant.IsAlly,
				combatant.Active))
			.ToArray();

		CombatViewModel model = new(
			view.BattlefieldWidth,
			view.BattlefieldHeight,
			combatants,
			decision.ActiveCombatantId,
			HasArtBackground: false,
			RoundNumber: view.RoundNumber,
			InitiativeOrder: initiativeOrder);

		return new RealCombatSnapshot(
			model,
			isPlayerTurn,
			isPlayerTurn ? decision.Movement!.DestinationOptions : Array.Empty<CombatMovementDestinationOption>(),
			isPlayerTurn ? decision.WeaponAttacks : Array.Empty<CombatWeaponAttackOption>(),
			isPlayerTurn ? decision.SpellAttacks : Array.Empty<CombatSpellAttackOption>(),
			isPlayerTurn && decision.Disengage!.IsAvailable,
			isPlayerTurn && decision.EndTurn!.IsAvailable,
			isCompleted,
			statusMessage);
	}

	private static void DescribeStep(
		CombatStepResult step,
		IReadOnlyDictionary<string, string> displayNames,
		List<string> lines)
	{
		switch (step.Kind)
		{
			case CombatStepKind.WeaponAttack:
				lines.Add(DescribeWeaponAttack(step, displayNames));
				break;
			case CombatStepKind.SpellAttack:
				lines.Add(DescribeSpellAttack(step, displayNames));
				break;
			case CombatStepKind.DeathSavingThrow:
				lines.Add(DescribeDeathSavingThrow(step, displayNames));
				break;
			case CombatStepKind.TurnAdvanced:
				lines.Add(
					$"{DescribeLabel(displayNames, step.TurnAdvancement!.EndedTurnCombatantId)}'s turn ends.");
				break;
			case CombatStepKind.Disengage:
				lines.Add(
					$"{DescribeLabel(displayNames, step.ActorCombatantId!)} disengages.");
				break;
			case CombatStepKind.CombatCompleted:
				break;
		}
	}

	private static string DescribeWeaponAttack(
		CombatStepResult step,
		IReadOnlyDictionary<string, string> displayNames)
	{
		CombatWeaponAttackStepDetail attack = step.WeaponAttack!;
		string actor = DescribeLabel(displayNames, step.ActorCombatantId!);
		string target = DescribeLabel(displayNames, step.TargetCombatantId!);

		// An opportunity attack has to read as one. Narrated identically to
		// an ordinary swing, it looks like an enemy who already acted just
		// attacked again out of turn -- which is exactly the complaint a
		// player would (reasonably) raise as a bug.
		string verb = attack.IsOpportunityAttack
			? "takes an opportunity attack against"
			: "attacks";

		if (attack.Outcome == AttackRollOutcome.Miss)
		{
			return $"{actor} {verb} {target} and misses.";
		}

		string hitWord = attack.Outcome == AttackRollOutcome.CriticalHit
			? "critically hits"
			: "hits";
		string opening = attack.IsOpportunityAttack
			? $"{actor} takes an opportunity attack and {hitWord} {target}"
			: $"{actor} {hitWord} {target}";
		string result = $"{opening} for {attack.FinalDamage} damage.";

		return AppendDownSuffix(result, target, attack.DamagedTarget);
	}

	// spell.AttackRollMode set means Core resolved it like a weapon attack
	// (Fire Bolt); spell.SaveAbility set means a saving throw (Sacred
	// Flame); neither set means it just happens (Cure Wounds, Healing
	// Word, Magic Missile, single-target Bless) — CombatSpellAttackStepDetail's
	// own doc comment guarantees at most one of the two is ever set.
	private static string DescribeSpellAttack(
		CombatStepResult step,
		IReadOnlyDictionary<string, string> displayNames)
	{
		CombatSpellAttackStepDetail spell = step.SpellAttack!;
		string actor = DescribeLabel(displayNames, step.ActorCombatantId!);
		string target = DescribeLabel(displayNames, step.TargetCombatantId!);

		if (spell.AttackRollMode is not null)
		{
			if (spell.AttackOutcome == AttackRollOutcome.Miss)
			{
				return $"{actor} casts {spell.SpellName} at {target} and misses.";
			}

			string hitWord = spell.AttackOutcome == AttackRollOutcome.CriticalHit
				? "critically hits"
				: "hits";
			return AppendDownSuffix(
				$"{actor} {hitWord} {target} with {spell.SpellName} for {spell.DamageDealt} damage.",
				target,
				spell.DamagedTarget);
		}

		if (spell.SaveAbility is not null)
		{
			string outcome = spell.SaveOutcome == D20TestOutcome.Failure
				? "fails"
				: "succeeds on";
			string result =
				$"{actor} casts {spell.SpellName} on {target} — {target} {outcome} the save";
			result += spell.DamageDealt > 0
				? $", taking {spell.DamageDealt} damage."
				: ".";

			return AppendDownSuffix(result, target, spell.DamagedTarget);
		}

		if (spell.HealingDone > 0)
		{
			return $"{actor} casts {spell.SpellName} on {target}, healing {spell.HealingDone} HP.";
		}

		if (spell.DamageDealt > 0)
		{
			return AppendDownSuffix(
				$"{actor} casts {spell.SpellName} on {target} for {spell.DamageDealt} damage.",
				target,
				spell.DamagedTarget);
		}

		return $"{actor} casts {spell.SpellName} on {target}.";
	}

	private static string DescribeDeathSavingThrow(
		CombatStepResult step,
		IReadOnlyDictionary<string, string> displayNames)
	{
		CombatDeathSavingThrowStepDetail deathSave = step.DeathSavingThrow!;
		string actor = DescribeLabel(displayNames, step.ActorCombatantId!);
		string tally =
			$"{deathSave.SuccessCount} success{(deathSave.SuccessCount == 1 ? string.Empty : "es")}, " +
			$"{deathSave.FailureCount} failure{(deathSave.FailureCount == 1 ? string.Empty : "s")}";

		return deathSave.Outcome switch
		{
			DeathSavingThrowOutcome.RegainedHitPoint =>
				$"{actor} rolls a natural 20 on a death save and returns to the fight with 1 hit point!",
			DeathSavingThrowOutcome.Stabilized =>
				$"{actor} stabilizes.",
			DeathSavingThrowOutcome.Dead =>
				$"{actor} fails a third death save and dies.",
			DeathSavingThrowOutcome.Success =>
				$"{actor} succeeds on a death save ({tally}).",
			DeathSavingThrowOutcome.Failure =>
				$"{actor} fails a death save ({tally}).",
			_ => $"{actor} rolls a death save.",
		};
	}

	private static string AppendDownSuffix(
		string message,
		string targetLabel,
		CombatDamagedTargetDetail? damagedTarget)
	{
		if (damagedTarget is
			{
				LifecycleState: CombatantLifecycleState.Dying
					or CombatantLifecycleState.Dead
					or CombatantLifecycleState.Defeated,
			})
		{
			return message + $" {targetLabel} is down!";
		}

		return message;
	}

	// CombatantView.DisplayName now carries a real name for every
	// combatant, party member and enemy alike (Application resolves the
	// enemy's name from the ruleset internally); this only falls back to
	// the raw ID if a combatant somehow isn't in the map this round-trip
	// captured, which should not happen for a live encounter.
	private static string DescribeLabel(
		IReadOnlyDictionary<string, string> displayNames,
		string combatantId)
	{
		return displayNames.TryGetValue(combatantId, out string? name)
			? name
			: combatantId;
	}
}

internal sealed record RealCombatSnapshot(
	CombatViewModel View,
	bool IsPlayerTurn,
	IReadOnlyList<CombatMovementDestinationOption> MoveDestinations,
	IReadOnlyList<CombatWeaponAttackOption> WeaponAttacks,
	IReadOnlyList<CombatSpellAttackOption> SpellAttacks,
	bool CanDisengage,
	bool CanEndTurn,
	bool IsCompleted,
	string? StatusMessage);
