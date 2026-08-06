using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Combat;
using Godot;

// The real integration seam's own combat handling — split out of
// ShellInteractionController.cs the same way .RealSession.cs/.Combat.cs
// were, once combat needed its own command bar and targeting flow driven
// by real backend state instead of MockCombatContent. Scoped to
// single-target actions: Move, Weapon Attack, Spell Attack, End Turn —
// multi-target spells are proven at the backend (RealCombatSession could
// be extended to reach them) but not wired here.
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
	// Captured once, when move targeting opens, rather than re-querying
	// RealCombatSession.Describe() on every cursor move -- the keyboard
	// cursor can fire this several times a second, and the battlefield's
	// own combatant positions don't change mid-targeting anyway.
	private HashSet<(int X, int Y)>? _pendingRealMoveOccupiedPositions;
	private IReadOnlyList<CombatWeaponAttackOption>? _pendingRealWeaponAttacks;

	// Set together with _pendingRealCombatCommand == "cast" — which spell
	// is being targeted and its own legal targets. Unlike weapon attacks
	// (usually one weapon, so flattening targets across every available
	// weapon is unambiguous), a caster routinely knows several spells with
	// overlapping legal targets, so each spell gets its own command-bar
	// button and its own single-spell targeting pass rather than a
	// flattened, ambiguous one.
	private string? _pendingRealSpellId;
	private string? _pendingRealSpellName;
	private IReadOnlyList<CombatTargetOption>? _pendingRealSpellTargets;

	// Called from ShellInteractionController.RealSession.cs's
	// ApplicationMode.Encounter case (isNewCombat true or false depending
	// on whether _activeCombatSession was just constructed) and from
	// ContinueRealCombat below (always isNewCombat: false, since that path
	// only ever runs against an already-active encounter). Renders whatever
	// RealCombatSession.Describe() reports through the same CombatView the
	// mock content uses, or hands off to ConcludeRealCombat once the fight
	// is over.
	private void ShowRealCombat(
		RealSessionSnapshot snapshot,
		IReadOnlyList<string>? lines,
		bool isNewCombat)
	{
		RealCombatSnapshot combatSnapshot = _activeCombatSession!.Describe();

		if (combatSnapshot.IsCompleted)
		{
			ConcludeRealCombat(lines);
			return;
		}

		_presentationController.ConfigureCombat(combatSnapshot.View);
		_presentationController.SetHeader(snapshot.LocationDisplayName, "Combat");

		if (isNewCombat)
		{
			List<string> opening = new() { "A fight has begun!" };
			opening.AddRange(_activeCombatSession.OpeningLines);
			_presentationController.AppendJournal(opening);
		}
		else if (lines is { Count: > 0 })
		{
			_presentationController.AppendJournal(lines);
		}

		ShowPartyPreviewFromCombat(combatSnapshot);
		ShowRealCombatCommands(combatSnapshot);
	}

	// Re-renders an already-active encounter after a player action, the
	// combat-log-carrying counterpart to ShowRealSession's own string-
	// based overload -- used by every real combat action (Move, Attack,
	// Cast, End Turn) instead of that generic entry point, since those all
	// have RealCombatSession's own per-line narration available rather
	// than a single pre-joined message.
	private void ContinueRealCombat(IReadOnlyList<string> lines)
	{
		ResetContext();

		RealGameSession session = _activeCombatGameSession!;
		_activeRealSession = session;
		RefreshPartyPreview();

		RealSessionSnapshot snapshot = session.Describe();
		ShowRealCombat(snapshot, lines, isNewCombat: false);
	}

	// The sidebar's own live HP during a fight -- ShowRealSession's own
	// party-preview refresh reads RealGameSession, which mid-combat HP
	// changes don't reach until the fight ends (ResumeFromCombat), so
	// combat needs its own refresh sourced from the battlefield's live
	// CombatantMarkerViewModels instead.
	private void ShowPartyPreviewFromCombat(RealCombatSnapshot combatSnapshot)
	{
		PartyViewModel party = new(combatSnapshot.View.Combatants
			.Where(combatant => combatant.IsAlly)
			.Select(combatant => new PartyMemberViewModel(
				combatant.Id,
				combatant.Label,
				$"{combatant.CurrentHitPoints} / {combatant.MaximumHitPoints}",
				combatant.MaximumHitPoints is null or 0
					? 0.0
					: (double)(combatant.CurrentHitPoints ?? 0) /
						combatant.MaximumHitPoints.Value))
			.ToArray());

		_partyPreview.ShowParty(ApplyPartyHighlight(party));
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

		// A single "Cast" opening a list overlay, not one button per spell
		// — flattening every prepared spell onto the bar directly (the
		// original approach here) doesn't scale past a spell or two. See
		// EnterRealCombatSpellMenu for where the list itself lives.
		if (combatSnapshot.SpellAttacks.Any(spell => spell.IsAvailable))
		{
			commands.Add(new CommandDefinition(
				"Cast",
				"[b]C[/b]ast",
				Key.C,
				() => EnterRealCombatSpellMenu(combatSnapshot)));
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

	// Reuses the same ModalScreenView/ShowModalScreen plumbing every M9
	// secondary screen (Character, Inventory, ...) already goes through,
	// rather than a bespoke overlay — its list rows render through
	// SelectionList, which is click-or-Up/Down-plus-Enter only (list rows
	// never read CommandViewModel.Hotkey, unlike the footer Commands row),
	// matching the requirement that the spell list itself carry no
	// per-item hotkeys. Closing before entering targeting matters:
	// EnterRealCombatSpellTargeting pushes ShellInteractionContext.
	// Targeting, and PopContext only pops when the context on top matches
	// what's expected, so ModalScreen has to already be off the stack
	// first — CloseModalScreen's CloseScreen() triggers that synchronously
	// via ModalScreenView's Closed signal.
	private void EnterRealCombatSpellMenu(RealCombatSnapshot combatSnapshot)
	{
		List<CommandViewModel> spellRows = combatSnapshot.SpellAttacks
			.Where(spell => spell.IsAvailable)
			.Select(spell => new CommandViewModel(spell.SpellId, spell.SpellName))
			.ToList();

		ShowModalScreen(
			new ModalViewModel("Cast a Spell", ListItems: spellRows),
			new Dictionary<string, Action> { ["close"] = CloseModalScreen },
			onRowActivated: spellId =>
			{
				CloseModalScreen();
				EnterRealCombatSpellTargeting(combatSnapshot, spellId);
			});
	}

	// A full-battlefield keyboard cursor, not a pre-highlighted set of
	// legal destinations -- one CombatHighlightCell per grid cell,
	// "move-legal" for a real CombatMovementDestinationOption's own
	// position and "move-illegal" for everything else, so the cursor
	// can move anywhere and read legal/illegal per tile it's actually
	// on, matching the old Gold Box games' own tile-cursor movement
	// instead of lighting up the whole reachable zone at once.
	private void EnterRealCombatMoveTargeting(RealCombatSnapshot combatSnapshot)
	{
		_pendingRealCombatCommand = "move";
		_pendingRealMoveDestinations = combatSnapshot.MoveDestinations;
		_pendingRealMoveOccupiedPositions = combatSnapshot.View.Combatants
			.Select(combatant => (combatant.GridX, combatant.GridY))
			.ToHashSet();

		HashSet<(int X, int Y)> legalPositions = combatSnapshot.MoveDestinations
			.Select(destination => (destination.Destination.X, destination.Destination.Y))
			.ToHashSet();
		List<CombatHighlightViewModel> cursorCells = new();

		for (int y = 0; y < combatSnapshot.View.GridHeight; y++)
		{
			for (int x = 0; x < combatSnapshot.View.GridWidth; x++)
			{
				cursorCells.Add(new CombatHighlightViewModel(
					x,
					y,
					legalPositions.Contains((x, y)) ? "move-legal" : "move-illegal"));
			}
		}

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(cursorCells);
		_presentationController.SetMessage(
			"Choose a destination. Press Esc to cancel.");
	}

	// Reports why the cursor's current tile isn't a legal destination --
	// "space occupied" is real, derived from the same combatant
	// positions the cursor grid itself was built from; anything else is
	// reported as simply out of reach, since the client has no way to
	// tell "too far" apart from "blocked terrain" without the engine
	// explaining itself, which it doesn't do today (CombatMovement
	// DestinationOption is only ever a positive list of reachable tiles,
	// never a reason a given tile is missing from it). A real "explain
	// this tile" endpoint would be Application-layer work, not attempted
	// here.
	private void ResolveRealCombatCursorFocused(int gridX, int gridY)
	{
		if (_pendingRealCombatCommand != "move" ||
			_pendingRealMoveDestinations is null ||
			_pendingRealMoveOccupiedPositions is null)
		{
			return;
		}

		bool isLegal = _pendingRealMoveDestinations.Any(
			destination => destination.Destination.X == gridX &&
				destination.Destination.Y == gridY);

		if (isLegal)
		{
			_presentationController.SetMessage(
				"Choose a destination. Press Esc to cancel.");
			return;
		}

		string reason = _pendingRealMoveOccupiedPositions.Contains((gridX, gridY))
			? "Space occupied."
			: "Out of reach.";

		_presentationController.SetMessage($"{reason} Press Esc to cancel.");
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

	// One spell's own Targets only — no cross-option flattening needed,
	// since each spell already got its own command-bar button (see
	// ShowRealCombatCommands).
	private void EnterRealCombatSpellTargeting(
		RealCombatSnapshot combatSnapshot,
		string spellId)
	{
		CombatSpellAttackOption? spell = combatSnapshot.SpellAttacks
			.FirstOrDefault(candidate => candidate.SpellId == spellId);

		if (spell is null)
		{
			return;
		}

		_pendingRealCombatCommand = "cast";
		_pendingRealSpellId = spellId;
		_pendingRealSpellName = spell.SpellName;
		_pendingRealSpellTargets = spell.Targets;

		Dictionary<string, CombatantMarkerViewModel> combatantsById =
			combatSnapshot.View.Combatants
				.ToDictionary(combatant => combatant.Id, StringComparer.Ordinal);
		List<CombatHighlightViewModel> highlights = new();

		foreach (CombatTargetOption target in spell.Targets)
		{
			if (!target.IsAvailable)
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

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(highlights);
		_presentationController.SetMessage(
			$"Choose a target for {spell.SpellName}. Press Esc to cancel.");
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

		IReadOnlyList<string> lines = _activeCombatSession!.SubmitMove(match.Path);
		ContinueRealCombat(lines);
	}

	// Called from ShellInteractionController.Combat.cs's
	// OnCombatantTargeted, ahead of the mock-content handling.
	private void ResolveRealCombatantTargeted(string combatantId)
	{
		if (_pendingRealCombatCommand == "cast")
		{
			ResolveRealSpellTargeted(combatantId);
			return;
		}

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

	private void ResolveRealSpellTargeted(string combatantId)
	{
		if (_pendingRealSpellId is not string spellId ||
			_pendingRealSpellTargets is null)
		{
			return;
		}

		bool isLegalTarget = _pendingRealSpellTargets.Any(
			candidate => candidate.IsAvailable &&
				candidate.TargetCombatantId == combatantId);

		if (!isLegalTarget)
		{
			return;
		}

		string spellName = _pendingRealSpellName ?? spellId;

		PopContext(ShellInteractionContext.Targeting);
		ClearRealCombatTargeting();
		ShowRealSpellCastConfirmation(spellId, spellName, combatantId);
	}

	// Shared by both cancel paths (Esc via ShellInputRouter's
	// CancelCombatTargeting and a resolved targeting choice above) — the
	// same "clear pending state and the highlight overlay in one place"
	// discipline .Combat.cs's own mock targeting already follows.
	private void ClearRealCombatTargeting()
	{
		_pendingRealCombatCommand = null;
		_pendingRealMoveDestinations = null;
		_pendingRealMoveOccupiedPositions = null;
		_pendingRealWeaponAttacks = null;
		_pendingRealSpellId = null;
		_pendingRealSpellName = null;
		_pendingRealSpellTargets = null;
		_presentationController.ShowCombatHighlights(null);
	}

	private void SubmitRealEndTurn()
	{
		IReadOnlyList<string> lines = _activeCombatSession!.SubmitEndTurn();
		ContinueRealCombat(lines);
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
				IReadOnlyList<string> lines = _activeCombatSession!.SubmitWeaponAttack(
					weaponId, targetCombatantId);
				ContinueRealCombat(lines);
			},
			onClosed: () =>
			{
				PopContext(ShellInteractionContext.Confirmation);
				_presentationController.SelectCombatTarget(null);
			});
	}

	// Mirrors ShowRealAttackConfirmation exactly, for a spell cast instead
	// of a weapon attack.
	private void ShowRealSpellCastConfirmation(
		string spellId,
		string spellName,
		string targetCombatantId)
	{
		PushContext(ShellInteractionContext.Confirmation);
		_presentationController.SelectCombatTarget(targetCombatantId);

		_confirmation.ShowConfirmation(
			"Cast",
			$"Cast {spellName} on {targetCombatantId}?",
			"Cast",
			"Cancel",
			onConfirmed: () =>
			{
				IReadOnlyList<string> lines = _activeCombatSession!.SubmitSpellAttack(
					spellId, targetCombatantId);
				ContinueRealCombat(lines);
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
	// finalNarration carries whatever the round that just completed the
	// fight actually did -- the last attacks, deaths, and (most often, since
	// EncounterCompletionRules only completes once every dying combatant has
	// resolved) death saves. It's appended to the journal alongside the
	// outcome line as its own entries rather than folded into one string --
	// the detail lives in permanent history now, so the transient status
	// line ShowRealSession shows next only needs the terse outcome.
	// AppendJournal runs AFTER ShowRealSession for the same reason
	// SubmitRealCommand's own does -- ShowRealSession's non-combat
	// branches end in SetMessage, which would otherwise clobber the
	// journal's full-list render back down to one line.
	private void ConcludeRealCombat(IReadOnlyList<string>? finalNarration)
	{
		RealGameSession session = _activeCombatGameSession!;
		CombatOutcomeResult outcome =
			CombatOutcomeRules.Finalize(_activeCombatSession!.State);

		_activeCombatSession = null;
		_activeCombatGameSession = null;

		session.ResumeFromCombat(outcome.State);

		string outcomeLine = outcome.Outcome == CombatOutcome.PartyVictory
			? "Victory! The raiders are defeated."
			: "Defeat...";

		List<string> journalLines = new();

		if (finalNarration is { Count: > 0 })
		{
			journalLines.AddRange(finalNarration);
		}

		journalLines.Add(outcomeLine);

		ShowRealSession(session, outcomeLine);
		_presentationController.AppendJournal(journalLines);
	}
}
