using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Application.Parties;
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
// Attack, End Turn. Multi-target spells (Bless's TargetCombinations) and
// any player-facing death-saving-throw decision are proven at the backend
// but not wired here yet.
internal sealed class RealCombatSession
{
	private ApplicationSessionState _state;

	// Captured once, at construction, from the party roster the session
	// already carries — the same "capture once at start, reuse for the
	// duration" precedent RealGameSession sets for
	// _travelOriginDisplayName/_travelDestinationDisplayName at
	// BeginJourney. EncounterPartySideResolver/PartySideId are internal to
	// Application and not visible here; this ID set is the actually-
	// available mechanism, and it's exactly what CombatOperations.Query
	// computes internally for the same purpose.
	private readonly HashSet<string> _partyMemberIds;

	internal RealCombatSession(ApplicationSessionState state)
	{
		_partyMemberIds = state.Party.Members
			.Select(member => member.PartyMemberId)
			.ToHashSet(StringComparer.Ordinal);

		// Runs any pre-player automatic processing (e.g. a surprise round)
		// before the first snapshot is ever described — the same thing
		// Console's RunCombatSession does immediately on entry.
		CombatResolutionResult opening =
			CombatOperations.AdvanceToDecision(state);
		_state = opening.State;
	}

	internal ApplicationSessionState State => _state;

	internal RealCombatSnapshot Describe()
	{
		return Describe(statusMessage: null);
	}

	internal string SubmitMove(IReadOnlyList<GridPosition> path)
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

	internal string SubmitWeaponAttack(string weaponId, string targetCombatantId)
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

	internal string SubmitEndTurn()
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
	internal string SubmitSpellAttack(string spellId, string targetCombatantId)
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
	private string Advance(CombatResolutionResult submitted)
	{
		CombatResolutionResult settled =
			CombatOperations.AdvanceToDecision(submitted.State);
		_state = settled.State;

		List<string> lines = new();

		if (submitted.PrimaryStep is not null)
		{
			DescribeStep(submitted.PrimaryStep, lines);
		}

		foreach (CombatStepResult step in submitted.AutomaticSteps)
		{
			DescribeStep(step, lines);
		}

		foreach (CombatStepResult step in settled.AutomaticSteps)
		{
			DescribeStep(step, lines);
		}

		return lines.Count == 0
			? "Nothing happened."
			: string.Join(" ", lines);
	}

	private RealCombatSnapshot Describe(string? statusMessage)
	{
		EncounterView view = CombatOperations.Query(_state);
		CombatDecision decision = view.Decision;

		IReadOnlyList<CombatantMarkerViewModel> combatants = view.Combatants
			.Select(combatant => new CombatantMarkerViewModel(
				combatant.CombatantId,
				DescribeLabel(combatant.CombatantId),
				combatant.Position.X,
				combatant.Position.Y,
				Active: combatant.CombatantId == decision.ActiveCombatantId,
				IsAlly: _partyMemberIds.Contains(combatant.CombatantId),
				CurrentHitPoints: combatant.Health.HitPoints.CurrentHitPoints,
				MaximumHitPoints: combatant.Health.HitPoints.MaximumHitPoints))
			.ToArray();

		bool isCompleted = decision.State == CombatDecisionState.CombatCompleted;
		bool isPlayerTurn = decision.State == CombatDecisionState.PlayerDecisionRequired;

		CombatViewModel model = new(
			view.BattlefieldWidth,
			view.BattlefieldHeight,
			combatants,
			decision.ActiveCombatantId,
			HasArtBackground: false);

		return new RealCombatSnapshot(
			model,
			isPlayerTurn,
			isPlayerTurn ? decision.Movement!.DestinationOptions : Array.Empty<CombatMovementDestinationOption>(),
			isPlayerTurn ? decision.WeaponAttacks : Array.Empty<CombatWeaponAttackOption>(),
			isPlayerTurn ? decision.SpellAttacks : Array.Empty<CombatSpellAttackOption>(),
			isPlayerTurn && decision.EndTurn!.IsAvailable,
			isCompleted,
			statusMessage);
	}

	private void DescribeStep(CombatStepResult step, List<string> lines)
	{
		switch (step.Kind)
		{
			case CombatStepKind.WeaponAttack:
				lines.Add(DescribeWeaponAttack(step));
				break;
			case CombatStepKind.SpellAttack:
				lines.Add(DescribeSpellAttack(step));
				break;
			case CombatStepKind.TurnAdvanced:
				lines.Add(
					$"{DescribeLabel(step.TurnAdvancement!.EndedTurnCombatantId)}'s turn ends.");
				break;
			case CombatStepKind.CombatCompleted:
				break;
		}
	}

	private string DescribeWeaponAttack(CombatStepResult step)
	{
		CombatWeaponAttackStepDetail attack = step.WeaponAttack!;
		string actor = DescribeLabel(step.ActorCombatantId!);
		string target = DescribeLabel(step.TargetCombatantId!);

		if (attack.Outcome == AttackRollOutcome.Miss)
		{
			return $"{actor} attacks {target} and misses.";
		}

		string hitWord = attack.Outcome == AttackRollOutcome.CriticalHit
			? "critically hits"
			: "hits";
		string result = $"{actor} {hitWord} {target} for {attack.FinalDamage} damage.";

		return AppendDownSuffix(result, target, attack.DamagedTarget);
	}

	// spell.AttackRollMode set means Core resolved it like a weapon attack
	// (Fire Bolt); spell.SaveAbility set means a saving throw (Sacred
	// Flame); neither set means it just happens (Cure Wounds, Healing
	// Word, Magic Missile, single-target Bless) — CombatSpellAttackStepDetail's
	// own doc comment guarantees at most one of the two is ever set.
	private string DescribeSpellAttack(CombatStepResult step)
	{
		CombatSpellAttackStepDetail spell = step.SpellAttack!;
		string actor = DescribeLabel(step.ActorCombatantId!);
		string target = DescribeLabel(step.TargetCombatantId!);

		if (spell.AttackRollMode is not null)
		{
			if (spell.AttackOutcome == AttackRollOutcome.Miss)
			{
				return $"{actor} casts {spell.SpellId} at {target} and misses.";
			}

			string hitWord = spell.AttackOutcome == AttackRollOutcome.CriticalHit
				? "critically hits"
				: "hits";
			return AppendDownSuffix(
				$"{actor} {hitWord} {target} with {spell.SpellId} for {spell.DamageDealt} damage.",
				target,
				spell.DamagedTarget);
		}

		if (spell.SaveAbility is not null)
		{
			string outcome = spell.SaveOutcome == D20TestOutcome.Failure
				? "fails"
				: "succeeds on";
			string result =
				$"{actor} casts {spell.SpellId} on {target} — {target} {outcome} the save";
			result += spell.DamageDealt > 0
				? $", taking {spell.DamageDealt} damage."
				: ".";

			return AppendDownSuffix(result, target, spell.DamagedTarget);
		}

		if (spell.HealingDone > 0)
		{
			return $"{actor} casts {spell.SpellId} on {target}, healing {spell.HealingDone} HP.";
		}

		if (spell.DamageDealt > 0)
		{
			return AppendDownSuffix(
				$"{actor} casts {spell.SpellId} on {target} for {spell.DamageDealt} damage.",
				target,
				spell.DamagedTarget);
		}

		return $"{actor} casts {spell.SpellId} on {target}.";
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

	// Party member labels use the real, public display name
	// (PartyMemberState.DisplayName). Enemy combatants have no public
	// display name — CombatantDefinition.DisplayName lives on the
	// internal EncounterDefinition — so this falls back to the raw
	// combatant ID, the same limitation Console has always had.
	private string DescribeLabel(string combatantId)
	{
		PartyMemberState? member = _state.Party.Members
			.FirstOrDefault(candidate => candidate.PartyMemberId == combatantId);

		return member?.DisplayName ?? combatantId;
	}
}

internal sealed record RealCombatSnapshot(
	CombatViewModel View,
	bool IsPlayerTurn,
	IReadOnlyList<CombatMovementDestinationOption> MoveDestinations,
	IReadOnlyList<CombatWeaponAttackOption> WeaponAttacks,
	IReadOnlyList<CombatSpellAttackOption> SpellAttacks,
	bool CanEndTurn,
	bool IsCompleted,
	string? StatusMessage);
