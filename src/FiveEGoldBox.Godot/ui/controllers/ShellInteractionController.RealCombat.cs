using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Combat;
using Godot;

// The real integration seam's own combat handling — split out of
// ShellInteractionController.cs the same way .RealSession.cs/.Combat.cs
// were, once combat needed its own command bar and targeting flow driven
// by real backend state instead of MockCombatContent. Scoped to the
// vertical slice's basics: Move, single-target Weapon Attack, End Turn —
// spellcasting and multi-target attacks are proven at the backend
// (RealCombatSession could be extended to reach them) but not wired here.
internal sealed partial class ShellInteractionController
{
	// Both fields live for exactly one encounter's duration, set together
	// the moment ShowRealSession first sees ApplicationMode.Encounter and
	// cleared together the moment ConcludeRealCombat hands the session
	// back. RealGameSession itself never learns CombatOperations exists —
	// only this controller and RealCombatSession do.
	private RealGameSession? _activeCombatGameSession;
	private RealCombatSession? _activeCombatSession;

	// Which real-combat targeting flow is open — null outside one. Mirrors
	// _pendingCombatCommand's role in .Combat.cs for the mock path.
	private string? _pendingRealCombatCommand;
	private IReadOnlyList<CombatMovementDestinationOption>? _pendingRealMoveDestinations;
	private IReadOnlyList<CombatWeaponAttackOption>? _pendingRealWeaponAttacks;

	// Called from ShellInteractionController.RealSession.cs's
	// ApplicationMode.Encounter case. Renders whatever RealCombatSession.
	// Describe() reports through the same CombatView the mock content
	// uses, or hands off to ConcludeRealCombat once the fight is over.
	private void ShowRealCombat(RealSessionSnapshot snapshot, string? overrideMessage)
	{
		RealCombatSnapshot combatSnapshot = _activeCombatSession!.Describe();

		if (combatSnapshot.IsCompleted)
		{
			ConcludeRealCombat();
			return;
		}

		_presentationController.ConfigureCombat(combatSnapshot.View);
		_presentationController.SetHeader(snapshot.LocationDisplayName, "Combat");
		_presentationController.SetMessage(overrideMessage ?? "A fight has begun!");
		ShowRealCombatCommands(combatSnapshot);
	}

	private void ShowRealCombatCommands(RealCombatSnapshot combatSnapshot)
	{
		List<CommandDefinition> commands = new();

		if (combatSnapshot.MoveDestinations.Count > 0)
		{
			commands.Add(new CommandDefinition(
				"Move",
				"[b]M[/b]ove",
				Key.M,
				() => EnterRealCombatMoveTargeting(combatSnapshot)));
		}

		if (combatSnapshot.WeaponAttacks.Any(weapon => weapon.IsAvailable))
		{
			commands.Add(new CommandDefinition(
				"Attack",
				"[b]A[/b]ttack",
				Key.A,
				() => EnterRealCombatAttackTargeting(combatSnapshot)));
		}

		if (combatSnapshot.CanEndTurn)
		{
			commands.Add(new CommandDefinition(
				"End Turn",
				"[b]E[/b]nd Turn",
				Key.E,
				SubmitRealEndTurn));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	private void EnterRealCombatMoveTargeting(RealCombatSnapshot combatSnapshot)
	{
		_pendingRealCombatCommand = "move";
		_pendingRealMoveDestinations = combatSnapshot.MoveDestinations;

		IReadOnlyList<CombatHighlightViewModel> highlights = combatSnapshot.MoveDestinations
			.Select(destination => new CombatHighlightViewModel(
				destination.Destination.X,
				destination.Destination.Y,
				"move-range"))
			.ToList();

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(highlights);
		_presentationController.SetMessage(
			"Choose a destination. Press Esc to cancel.");
	}

	// Highlights every legal (weapon, target) pair's target position,
	// deduplicated by target — a combatant carrying two weapons that can
	// both reach the same enemy still highlights that enemy once. Which
	// specific weapon fires is resolved later, in
	// ResolveRealCombatantTargeted, from whichever weapon actually lists
	// the clicked combatant as a legal target.
	private void EnterRealCombatAttackTargeting(RealCombatSnapshot combatSnapshot)
	{
		_pendingRealCombatCommand = "attack";
		_pendingRealWeaponAttacks = combatSnapshot.WeaponAttacks;

		Dictionary<string, CombatantMarkerViewModel> combatantsById =
			combatSnapshot.View.Combatants
				.ToDictionary(combatant => combatant.Id, StringComparer.Ordinal);
		HashSet<string> seenTargets = new(StringComparer.Ordinal);
		List<CombatHighlightViewModel> highlights = new();

		foreach (CombatWeaponAttackOption weapon in combatSnapshot.WeaponAttacks)
		{
			if (!weapon.IsAvailable)
			{
				continue;
			}

			foreach (CombatTargetOption target in weapon.Targets)
			{
				if (!target.IsAvailable ||
					!seenTargets.Add(target.TargetCombatantId))
				{
					continue;
				}

				if (combatantsById.TryGetValue(
					target.TargetCombatantId,
					out CombatantMarkerViewModel? combatant))
				{
					highlights.Add(new CombatHighlightViewModel(
						combatant.GridX, combatant.GridY, "valid-target"));
				}
			}
		}

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(highlights);
		_presentationController.SetMessage(
			"Choose a target to attack. Press Esc to cancel.");
	}

	// Called from ShellInteractionController.Combat.cs's
	// OnCombatCellTargeted, ahead of the mock-content handling, whenever
	// real combat owns the pending targeting flow.
	private void ResolveRealCombatCellTargeted(int gridX, int gridY)
	{
		if (_pendingRealCombatCommand != "move" ||
			_pendingRealMoveDestinations is null)
		{
			return;
		}

		CombatMovementDestinationOption? match = _pendingRealMoveDestinations
			.FirstOrDefault(destination =>
				destination.Destination.X == gridX &&
					destination.Destination.Y == gridY);

		if (match is null)
		{
			return;
		}

		PopContext(ShellInteractionContext.Targeting);
		ClearRealCombatTargeting();

		string message = _activeCombatSession!.SubmitMove(match.Path);
		ShowRealSession(_activeCombatGameSession!, message);
	}

	// Called from ShellInteractionController.Combat.cs's
	// OnCombatantTargeted, ahead of the mock-content handling.
	private void ResolveRealCombatantTargeted(string combatantId)
	{
		if (_pendingRealCombatCommand != "attack" ||
			_pendingRealWeaponAttacks is null)
		{
			return;
		}

		foreach (CombatWeaponAttackOption weapon in _pendingRealWeaponAttacks)
		{
			if (!weapon.IsAvailable)
			{
				continue;
			}

			CombatTargetOption? target = weapon.Targets.FirstOrDefault(
				candidate => candidate.IsAvailable &&
					candidate.TargetCombatantId == combatantId);

			if (target is null)
			{
				continue;
			}

			PopContext(ShellInteractionContext.Targeting);
			ClearRealCombatTargeting();
			ShowRealAttackConfirmation(weapon.WeaponId, combatantId);
			return;
		}
	}

	// Shared by both cancel paths (Esc via ShellInputRouter's
	// CancelCombatTargeting and a resolved targeting choice above) — the
	// same "clear pending state and the highlight overlay in one place"
	// discipline .Combat.cs's own mock targeting already follows.
	private void ClearRealCombatTargeting()
	{
		_pendingRealCombatCommand = null;
		_pendingRealMoveDestinations = null;
		_pendingRealWeaponAttacks = null;
		_presentationController.ShowCombatHighlights(null);
	}

	private void SubmitRealEndTurn()
	{
		string message = _activeCombatSession!.SubmitEndTurn();
		ShowRealSession(_activeCombatGameSession!, message);
	}

	// Reuses the same ConfirmationDialog mechanism M6e/M8e already
	// established for the one combat command with real consequence —
	// enemy combatants have no public display name to show here (see
	// RealCombatSession.DescribeLabel), so the raw combatant ID is what
	// the confirmation names, the same limitation Console has always had.
	private void ShowRealAttackConfirmation(string weaponId, string targetCombatantId)
	{
		PushContext(ShellInteractionContext.Confirmation);
		_presentationController.SelectCombatTarget(targetCombatantId);

		_confirmation.ShowConfirmation(
			"Attack",
			$"Attack {targetCombatantId}?",
			"Attack",
			"Cancel",
			onConfirmed: () =>
			{
				string message = _activeCombatSession!.SubmitWeaponAttack(
					weaponId, targetCombatantId);
				ShowRealSession(_activeCombatGameSession!, message);
			},
			onClosed: () =>
			{
				PopContext(ShellInteractionContext.Confirmation);
				_presentationController.SelectCombatTarget(null);
			});
	}

	// CombatOutcomeRules.Finalize already leaves the session in
	// Exploration (a win, restored to wherever the party was exploring
	// before the trigger fired) or ScenarioConclusion (Watchtower's only
	// declared conclusion, PartyDefeated) — routing back through
	// ShowRealSession's existing dispatch means neither case needs any
	// new presentation code here.
	private void ConcludeRealCombat()
	{
		RealGameSession session = _activeCombatGameSession!;
		CombatOutcomeResult outcome =
			CombatOutcomeRules.Finalize(_activeCombatSession!.State);

		_activeCombatSession = null;
		_activeCombatGameSession = null;

		session.ResumeFromCombat(outcome.State);

		string message = outcome.Outcome == CombatOutcome.PartyVictory
			? "Victory! The raiders are defeated."
			: "Defeat...";

		ShowRealSession(session, message);
	}
}
