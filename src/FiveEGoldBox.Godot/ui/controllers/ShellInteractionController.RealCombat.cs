using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Combat;
using FiveEGoldBox.Core.Rules;
using FiveEGoldBox.Core.Runtime;
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
	// A legal target's grid position -> its combatant id, captured once
	// when spell targeting opens (positions don't move mid-targeting, the
	// same reasoning _pendingRealMoveOccupiedPositions already relies on).
	// What the full-battlefield cursor resolves a cell activation/focus
	// against — see EnterRealCombatSpellTargeting's own comment on why
	// spell targeting drives its cursor from cells, not the pins.
	private Dictionary<(int X, int Y), string>? _pendingRealSpellTargetPositions;
	// Combatant id -> the display name the pins and the journal already
	// use, captured when either targeting flow opens. CombatTargetOption
	// carries only an id, and both the cursor preview and the confirmation
	// dialog want the real name rather than "combatant.mill-rat.first".
	private Dictionary<string, string>? _pendingRealTargetLabels;

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

		// Only offered while it can still change something: once the action
		// is spent (or already spent on disengaging), the button would be a
		// promise the engine refuses -- and this command bar's own
		// convention is that a command that exists is a command that works.
		if (combatSnapshot.CanDisengage)
		{
			commands.Add(new CommandDefinition(
				"Disengage",
				"[b]D[/b]isengage",
				Key.D,
				SubmitRealDisengage));
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
	// "cursor-legal" for a real CombatMovementDestinationOption's own
	// position and "cursor-illegal" for everything else, so the cursor
	// can move anywhere and read legal/illegal per tile it's actually
	// on, matching the old Gold Box games' own tile-cursor movement
	// instead of lighting up the whole reachable zone at once. Spell
	// targeting (EnterRealCombatSpellTargeting) reuses the same cell kind
	// for the same reason, once it needed the same shape.
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
					legalPositions.Contains((x, y)) ? "cursor-legal" : "cursor-illegal"));
			}
		}

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(cursorCells);

		// Starts the cursor on whoever's turn it is, per the user's own
		// request, rather than leaving it wherever Godot's default focus
		// happens to land.
		CombatantMarkerViewModel? activeCombatant = combatSnapshot.View.Combatants
			.FirstOrDefault(combatant => combatant.Active);

		if (activeCombatant is not null)
		{
			_presentationController.FocusCombatCell(
				activeCombatant.GridX, activeCombatant.GridY);
		}

		_presentationController.SetMessage(
			"Choose a destination. Press Esc to cancel.");
	}

	// Dispatches to whichever full-battlefield cursor is actually open --
	// move and spell targeting each report their own reason for the
	// current tile, since "not a legal move" and "not a legal spell
	// target" mean different things and come from different data. A real
	// attack targeting cursor never fires this at all (see CombatView.cs's
	// own comment on CellCursorFocused) — it still drives its cursor from
	// the pins, not these cells.
	private void ResolveRealCombatCursorFocused(int gridX, int gridY)
	{
		if (_pendingRealCombatCommand == "cast")
		{
			ResolveRealSpellCursorFocused(gridX, gridY);
			return;
		}

		ResolveRealMoveCursorFocused(gridX, gridY);
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
	private void ResolveRealMoveCursorFocused(int gridX, int gridY)
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

	// Weapon-attack targeting's own cursor report, the pin-driven twin of
	// ResolveRealMoveCursorFocused above. Only legal targets are focusable
	// while attack targeting is open (CombatView.ApplyCombatantFocusability),
	// so there is no illegal case to describe here — every pin the cursor
	// can reach is one this attack could actually be made against.
	//
	// Resolves the weapon the same way ResolveRealCombatantTargeted does —
	// first available weapon that lists this target — so the preview
	// describes the exact attack pressing Enter would make. Two weapons
	// reaching the same enemy can genuinely disagree (a bow shot from an
	// adjacent square has disadvantage where a sword swing does not), and a
	// preview drawn from a different weapon than the one that fires would
	// be worse than none.
	private void ResolveRealCombatantCursorFocused(string combatantId)
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

			_presentationController.SetMessage(
				$"{DescribeTargetPreview(combatantId, target)} Press Esc to cancel.");
			return;
		}
	}

	// What attacking or casting at this target actually costs, before the
	// player commits to it: how far away it is, whether the roll is swung
	// either way, and what its cover adds to what the roll has to beat.
	//
	// Every number is Core's own, computed for this exact actor/target/
	// weapon-or-spell triple by the same prerequisite evaluation that will
	// resolve the real roll a moment later — nothing here is re-derived or
	// estimated client-side, which is the whole reason it can be trusted.
	// Cover in particular has been computed and applied to every attack
	// since long before this; it simply never crossed the Application
	// boundary, so the player was hit by it without warning.
	//
	// Deliberately not a hit-chance percentage, which the Gold Box design
	// reference also asks for (§21.3): a blessed attacker's bonus is a d4,
	// so the real chance is a distribution rather than a figure, and
	// collapsing it to one number would be confidently wrong exactly when
	// a player leans on it hardest. The inputs are all shown instead.
	private string DescribeTargetPreview(
		string combatantId,
		CombatTargetOption target)
	{
		string label = ResolveTargetLabel(combatantId);
		List<string> parts = new();

		if (target.DistanceFeet is int distance)
		{
			parts.Add($"{distance} ft");
		}

		switch (target.AttackRollMode)
		{
			case D20RollMode.Advantage:
				parts.Add("advantage");
				break;
			case D20RollMode.Disadvantage:
				parts.Add("disadvantage");
				break;
		}

		if (DescribeCover(target) is string cover)
		{
			parts.Add(cover);
		}

		if (target.SaveDc is int saveDc && target.SaveAbility is Ability ability)
		{
			parts.Add($"DC {saveDc} {ability.ToString().ToUpperInvariant()[..3]} save");
		}

		return parts.Count == 0
			? $"{label}."
			: $"{label} — {string.Join(", ", parts)}.";
	}

	// Falls back to the raw id rather than throwing: a label map that
	// somehow doesn't name this combatant is a cosmetic problem, and the
	// id is exactly what every one of these call sites used to show.
	private string ResolveTargetLabel(string combatantId)
	{
		return _pendingRealTargetLabels is not null &&
			_pendingRealTargetLabels.TryGetValue(
				combatantId,
				out string? resolved)
			? resolved
			: combatantId;
	}

	// Null for no cover, so the caller can leave the phrase out entirely
	// rather than printing a reassuring "no cover" on every single target.
	// Which bonus is quoted follows how the target actually resists: an
	// attack roll has to clear the armor-class bonus, a saving-throw spell
	// has to clear the Dexterity save bonus. Both are on the same
	// evaluation; naming the wrong one would misstate the cost.
	private static string? DescribeCover(CombatTargetOption target)
	{
		if (target.Cover is not { } cover ||
			cover.CoverLevel == EncounterCoverLevel.None)
		{
			return null;
		}

		string level = cover.CoverLevel == EncounterCoverLevel.Half
			? "half cover"
			: "three-quarters cover";

		return target.SaveDc is not null
			? $"{level} (+{cover.DexteritySavingThrowBonus} to its save)"
			: $"{level} (+{cover.ArmorClassBonus} AC)";
	}

	// Reports whether the cursor's current tile is a legal target for the
	// spell being cast. Every spell today only ever resolves against a
	// specific creature, so there is only one reason a tile can be
	// illegal ("not a target") — unlike movement there's no occupied/
	// out-of-reach split to make, since standing on empty ground and
	// standing on the wrong creature both mean the same thing here. That
	// may need to grow a real reason once a spell can legally miss for a
	// specific cause (an ally-only spell aimed at an enemy, say).
	private void ResolveRealSpellCursorFocused(int gridX, int gridY)
	{
		if (_pendingRealSpellTargetPositions is null)
		{
			return;
		}

		string spellName = _pendingRealSpellName ?? "this spell";

		if (_pendingRealSpellTargetPositions.TryGetValue(
			(gridX, gridY),
			out string? targetCombatantId))
		{
			CombatTargetOption? target = _pendingRealSpellTargets?
				.FirstOrDefault(candidate =>
					candidate.IsAvailable &&
					candidate.TargetCombatantId == targetCombatantId);

			_presentationController.SetMessage(
				target is null
					? $"Choose a target for {spellName}. Press Esc to cancel."
					: $"{spellName} — {DescribeTargetPreview(targetCombatantId, target)} "
						+ "Press Esc to cancel.");
			return;
		}

		_presentationController.SetMessage(
			$"Not a valid target for {spellName}. Press Esc to cancel.");
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
		_pendingRealTargetLabels = CaptureTargetLabels(combatSnapshot);
		HashSet<string> seenTargets = new(StringComparer.Ordinal);
		List<CombatHighlightViewModel> highlights = new();
		List<string> targetIds = new();

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
					targetIds.Add(target.TargetCombatantId);
				}
			}
		}

		if (highlights.Count == 0)
		{
			// CombatWeaponAttackOption.IsAvailable is already "has a legal
			// target" (CombatViewFactory), so the command guard above
			// should make this unreachable. Kept because the failure mode
			// if it ever is reached -- a targeting context with nothing in
			// it and no way out but Esc -- is silent and baffling.
			_pendingRealCombatCommand = null;
			_pendingRealWeaponAttacks = null;
			_presentationController.SetMessage("No target within reach.");
			return;
		}

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(highlights);

		// Before SetCombatTargetableCombatants, not after: that call grabs
		// focus onto the first legal target, which synchronously fires
		// CombatantCursorFocused and writes the real per-target preview.
		// Setting the generic prompt afterwards would overwrite it with a
		// less useful line every single time targeting opened -- the same
		// append/overwrite ordering trap the journal wiring already hit.
		_presentationController.SetMessage(
			"Choose a target to attack. Press Esc to cancel.");
		_presentationController.SetCombatTargetableCombatants(targetIds);
	}

	// The display names for every combatant on the field, keyed by id.
	// Captured whole rather than filtered to the current legal targets --
	// it costs nothing, and both targeting flows plus the two confirmation
	// dialogs read from the same map.
	private static Dictionary<string, string> CaptureTargetLabels(
		RealCombatSnapshot combatSnapshot)
	{
		return combatSnapshot.View.Combatants.ToDictionary(
			combatant => combatant.Id,
			combatant => combatant.Label,
			StringComparer.Ordinal);
	}



	// A full-battlefield keyboard cursor, the same shape
	// EnterRealCombatMoveTargeting already built -- one CombatHighlightCell
	// per grid cell, "cursor-legal" for a legal target's own position and
	// "cursor-illegal" for everything else, rather than a sparse pin-cycle
	// restricted to today's known targets. Every spell currently in the
	// ruleset only ever resolves against a specific creature, so a legal
	// cell today is always exactly a target's square -- but the cursor
	// itself doesn't know that, and shouldn't: an eventual area-of-effect
	// spell needs to look anywhere on the field, not cycle a short target
	// list, and a spell that must hit one specific creature (Hold Person,
	// Charm Person, Cause Light Wounds -- none authored yet) is the
	// opposite, narrower case that will need its own restriction once it
	// exists. This is the free-roam shape; the pin-cycle shape real
	// weapon-attack targeting still uses is what a specific-target-only
	// spell would reuse instead, once one is authored.
	//
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
		_pendingRealTargetLabels = CaptureTargetLabels(combatSnapshot);

		Dictionary<string, CombatantMarkerViewModel> combatantsById =
			combatSnapshot.View.Combatants
				.ToDictionary(combatant => combatant.Id, StringComparer.Ordinal);
		Dictionary<(int X, int Y), string> targetPositions = new();
		(int X, int Y)? firstTargetPosition = null;

		foreach (CombatTargetOption target in spell.Targets)
		{
			if (!target.IsAvailable ||
				!combatantsById.TryGetValue(
					target.TargetCombatantId,
					out CombatantMarkerViewModel? combatant))
			{
				continue;
			}

			(int X, int Y) position = (combatant.GridX, combatant.GridY);
			targetPositions[position] = target.TargetCombatantId;
			firstTargetPosition ??= position;
		}

		_pendingRealSpellTargetPositions = targetPositions;

		List<CombatHighlightViewModel> cursorCells = new();

		for (int y = 0; y < combatSnapshot.View.GridHeight; y++)
		{
			for (int x = 0; x < combatSnapshot.View.GridWidth; x++)
			{
				cursorCells.Add(new CombatHighlightViewModel(
					x,
					y,
					targetPositions.ContainsKey((x, y)) ? "cursor-legal" : "cursor-illegal"));
			}
		}

		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(cursorCells);
		// An empty, non-null restriction — not the target-id list itself —
		// so every pin goes unfocusable and the cursor cells above are the
		// only thing arrow keys can reach. See CombatView.Markers.cs's
		// ApplyCombatantFocusability for why this has to be empty-and-
		// restricted rather than null-and-unrestricted. A direct mouse
		// click on a pin still resolves the cast via ResolveRealCombatant
		// Targeted/ResolveRealSpellTargeted regardless — Button.Pressed
		// fires on click independent of FocusMode.
		_presentationController.SetCombatTargetableCombatants(Array.Empty<string>());

		if (firstTargetPosition is (int startX, int startY))
		{
			// Deferred, not called directly like EnterRealCombatMoveTargeting's
			// own FocusCombatCell call -- this method is reached by way of
			// EnterRealCombatSpellMenu's onRowActivated, which calls
			// CloseModalScreen() immediately before this runs.
			// CloseModalScreen -> ModalBackdrop.CloseModal -> RestorePreviousFocus
			// already queued its own deferred GrabFocus, restoring focus to
			// whatever was focused before the spell list opened (the Cast
			// button) -- a synchronous GrabFocus here would win the moment
			// it's called, then immediately lose it when that queued call
			// runs at end of frame. Deferring this call too, queued strictly
			// after that one, makes it win instead: the same "last deferred
			// call in a frame wins" rule ModalScreenView.ShowScreen's own
			// comment already documents for the identical class of race.
			Callable.From(() =>
				_presentationController.FocusCombatCell(startX, startY)).CallDeferred();
		}

		_presentationController.SetMessage(
			$"Choose a target for {spell.SpellName}. Press Esc to cancel.");
	}

	// Called from ShellInteractionController.Combat.cs's
	// OnCombatCellTargeted, ahead of the mock-content handling, whenever
	// real combat owns the pending targeting flow. Move and spell
	// targeting both drive their cursor from full-battlefield cells now,
	// so both are resolved by grid position here rather than combatant id
	// — a real attack targeting resolves by id instead
	// (ResolveRealCombatantTargeted), since it still drives its cursor
	// from the pins.
	private void ResolveRealCombatCellTargeted(int gridX, int gridY)
	{
		if (_pendingRealCombatCommand == "cast")
		{
			ResolveRealSpellCellTargeted(gridX, gridY);
			return;
		}

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

	// The cursor-cell counterpart to ResolveRealSpellTargeted below —
	// looks up whichever combatant (if any) the activated cell belongs to
	// and defers to the exact same validation/confirmation path a direct
	// pin click already uses, so there's only one place that decides
	// whether a cast actually resolves.
	private void ResolveRealSpellCellTargeted(int gridX, int gridY)
	{
		if (_pendingRealSpellTargetPositions is null ||
			!_pendingRealSpellTargetPositions.TryGetValue(
				(gridX, gridY),
				out string? combatantId))
		{
			return;
		}

		ResolveRealSpellTargeted(combatantId);
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

			string targetLabel = ResolveTargetLabel(combatantId);

			PopContext(ShellInteractionContext.Targeting);
			ClearRealCombatTargeting();
			ShowRealAttackConfirmation(
				weapon.WeaponId,
				combatantId,
				targetLabel);
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
		string targetLabel = ResolveTargetLabel(combatantId);

		PopContext(ShellInteractionContext.Targeting);
		ClearRealCombatTargeting();
		ShowRealSpellCastConfirmation(
			spellId,
			spellName,
			combatantId,
			targetLabel);
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
		_pendingRealSpellTargetPositions = null;
		_pendingRealTargetLabels = null;
		_presentationController.ShowCombatHighlights(null);
		_presentationController.SetCombatTargetableCombatants(null);
	}

	// No targeting pass and no confirmation: unlike an attack it cannot be
	// aimed at the wrong thing, and unlike ending the turn it does not give
	// the turn away -- the worst case is spending an action, which every
	// other action button already does without asking.
	private void SubmitRealDisengage()
	{
		IReadOnlyList<string> lines = _activeCombatSession!.SubmitDisengage();
		ContinueRealCombat(lines);
	}

	private void SubmitRealEndTurn()
	{
		IReadOnlyList<string> lines = _activeCombatSession!.SubmitEndTurn();
		ContinueRealCombat(lines);
	}

	// Reuses the same ConfirmationDialog mechanism M6e/M8e already
	// established for the one combat command with real consequence. This
	// used to name the raw combatant ID, on the since-outdated belief that
	// an enemy had no display name to show -- CombatantView.DisplayName is
	// resolved for every combatant and already drives both the pins and
	// the journal, so the confirmation now says what the rest of the
	// screen says. Captured before the targeting state is cleared, since
	// ResolveRealCombatantTargeted clears it on the way in here.
	private void ShowRealAttackConfirmation(
		string weaponId,
		string targetCombatantId,
		string targetLabel)
	{
		PushContext(ShellInteractionContext.Confirmation);
		_presentationController.SelectCombatTarget(targetCombatantId);

		_confirmation.ShowConfirmation(
			"Attack",
			$"Attack {targetLabel}?",
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
		string targetCombatantId,
		string targetLabel)
	{
		PushContext(ShellInteractionContext.Confirmation);
		_presentationController.SelectCombatTarget(targetCombatantId);

		_confirmation.ShowConfirmation(
			"Cast",
			$"Cast {spellName} on {targetLabel}?",
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
