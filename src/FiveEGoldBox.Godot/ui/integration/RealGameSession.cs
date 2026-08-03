using System;
using System.Collections.Generic;
using System.Linq;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Outposts;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Application.Travel;
using FiveEGoldBox.Application.Views;

// The real integration seam. Wraps an actual ApplicationSessionState and
// projects it into the same Godot-owned view-model types the pre-integration
// mock content always produced (CommandSetViewModel/CommandViewModel), so
// the shell controllers render real and mock state through one pipeline.
//
// Scoped deliberately to what both sides already support: outpost
// decisions, exploration movement/triggers/stairs, and regional travel.
// Scenario conclusion gets its own authored epilogue via
// DescribeConclusion — see ShellInteractionController.ShowRealSession.
//
// Real SessionActionKinds do not match the seven-command exploration set
// (move/view/cast/area/encamp/search/look) MockExplorationCommandContent
// invented ahead of any backend capability. This deliberately does not
// pretend those five (view/cast/area/encamp/search/look) exist for a real
// session — only what SessionView actually reports is offered. That gap
// was exactly the kind of thing this integration pass exists to surface.
internal sealed class RealGameSession
{
	// No real per-scenario key exists for a regional map, same gap as
	// ExplorationSceneKeys — this is a placeholder key, not content.
	private const string RegionalMapKey = "real-session-region";

	private ApplicationSessionState _state;
	private Dictionary<string, SessionAction> _lastActions =
		new(StringComparer.Ordinal);

	// Captured the moment a journey begins, not read back off session state
	// later — CurrentLocationId only ever names the origin or the
	// destination (whichever the party currently is nearer being at), never
	// both at once, so there is nowhere else honest to read the far end's
	// name from mid-journey. Both come from the same content-authored
	// display names SessionView itself already computed.
	private string? _travelOriginDisplayName;
	private string? _travelDestinationDisplayName;

	internal RealGameSession(string scenarioId, int randomSeed)
	{
		_state = ScenarioSessionFactory.CreateNew(scenarioId, randomSeed);
	}

	// Lets a caller hand in an already-built session (e.g. one constructed
	// by ScenarioSessionFactory.CreateAtExploration for a debug jump-to-
	// location) rather than always starting a scenario fresh at its outpost.
	internal RealGameSession(ApplicationSessionState initialState)
	{
		_state = initialState;
	}

	// The state-ownership handoff to RealCombatSession
	// (ShellInteractionController.RealCombat.cs) — called once, the
	// moment ShowRealSession first sees ApplicationMode.Encounter. This
	// class stops being the active owner of _state until ResumeFromCombat
	// hands it back; it never learns CombatOperations exists, keeping the
	// combat dependency isolated to RealCombatSession alone.
	internal ApplicationSessionState HandOffToCombat() => _state;

	// Called once combat concludes (CombatOutcomeRules.Finalize has
	// already run) — resumes ownership of the post-combat state, which
	// Finalize already left in Exploration or ScenarioConclusion mode, so
	// the very next Describe() renders through the existing non-combat
	// switch cases with no special-casing needed here.
	internal void ResumeFromCombat(ApplicationSessionState state)
	{
		_state = state;
	}

	internal RealSessionSnapshot Describe()
	{
		SessionViewModel view = SessionView.Describe(_state);
		Dictionary<string, SessionAction> byCommandId =
			new(StringComparer.Ordinal);
		List<CommandViewModel> commands = new();
		HashSet<char> usedHotkeys = new();

		foreach (SessionAction action in view.Actions)
		{
			string commandId = "real." + commands.Count;

			byCommandId[commandId] = action;
			commands.Add(new CommandViewModel(
				commandId,
				action.DisplayName,
				HotkeyAssigner.Assign(
					action.DisplayName
						.Where(char.IsLetter)
						.Select(char.ToUpperInvariant),
					usedHotkeys,
					action.DisplayName)));
		}

		_lastActions = byCommandId;

		return new RealSessionSnapshot(
			view.Mode,
			view.LocationDisplayName,
			view.Mode.ToString(),
			DescribeStatus(view),
			view.IsConcluded,
			view.IsSuccess,
			new CommandSetViewModel(
				commands.ToArray(),
				ShellInteractionContext.CommandMenu),
			view.Mode == ApplicationMode.RegionalTravel
				? DescribeRegionalMap()
				: null);
	}

	// Grounded in RegionalTravelState's real CurrentStepIndex/FinalStepIndex
	// rather than invented geography — the backend has no concept of map
	// coordinates for a location, so this doesn't pretend to place things
	// on a meaningful map, only to show real journey progress between two
	// named points on a line. Positions are normalized (0.0-1.0), the same
	// convention MockRegionalMapContent uses (M7a), so RegionalMapView
	// renders both through the exact same Configure(RegionalMapViewModel)
	// — RegionalMapView itself switches off the calibrated Frontier
	// Region art when it sees this real MapKey (RegionalMapView.cs).
	private const double TrackOriginX = 0.08;
	private const double TrackDestinationX = 0.92;
	private const double TrackY = 0.5;

	private RegionalMapViewModel DescribeRegionalMap()
	{
		RegionalTravelState travel = _state.RegionalTravel
			?? throw new InvalidOperationException(
				"DescribeRegionalMap was called outside regional travel.");
		double progress = travel.FinalStepIndex == 0
			? 1.0
			: (double)travel.CurrentStepIndex / travel.FinalStepIndex;
		string originLabel = _travelOriginDisplayName ?? "Origin";
		string destinationLabel = _travelDestinationDisplayName ?? "Destination";
		double partyX = TrackOriginX + (progress * (TrackDestinationX - TrackOriginX));

		return new RegionalMapViewModel(
			RegionalMapKey,
			new RegionalMapMarkerViewModel(
				"party",
				"Party",
				new RegionalMapPointViewModel(partyX, TrackY)),
			new[]
			{
				new RegionalMapMarkerViewModel(
					travel.OriginLocationId,
					originLabel,
					new RegionalMapPointViewModel(TrackOriginX, TrackY)),
				new RegionalMapMarkerViewModel(
					travel.DestinationLocationId,
					destinationLabel,
					new RegionalMapPointViewModel(TrackDestinationX, TrackY),
					Selected: travel.IsComplete),
			},
			RoutePreview: new[]
			{
				new RegionalMapPointViewModel(TrackOriginX, TrackY),
				new RegionalMapPointViewModel(partyX, TrackY),
			});
	}

	// A read-only snapshot of the area map — real movement while it's
	// open still goes through SubmitMovement above, this just re-describes
	// afterward. ExplorationRules.Query is the public projection this
	// mirrors CombatOperations.Query for. Null whenever there's no
	// explorable floor to show (shouldn't happen while a caller is in
	// Exploration mode, but handled honestly rather than assumed away).
	internal AreaMapViewModel? DescribeAreaMap()
	{
		ExplorationMapView? map = ExplorationRules.Query(_state);

		if (map is null)
		{
			return null;
		}

		return new AreaMapViewModel(
			map.Width,
			map.Height,
			map.TraversablePositions
				.Select(position => new AreaMapPointViewModel(position.X, position.Y))
				.ToArray(),
			map.StairPositions
				.Select(position => new AreaMapPointViewModel(position.X, position.Y))
				.ToArray(),
			map.Doors
				.Select(door => new AreaMapDoorEdgeViewModel(
					new AreaMapPointViewModel(door.PositionA.X, door.PositionA.Y),
					new AreaMapPointViewModel(door.PositionB.X, door.PositionB.Y),
					door.IsLocked,
					door.IsOpen,
					door.IsRevealed))
				.ToArray(),
			map.TreasurePositions
				.Select(position => new AreaMapPointViewModel(position.X, position.Y))
				.ToArray(),
			map.PartyPosition.X,
			map.PartyPosition.Y,
			map.PartyFacing.ToString());
	}

	// A read-only snapshot of the party's shared purse/inventory, built
	// straight into the same ModalViewModel shape ShowInventoryScreen
	// already renders MockSecondaryScreenContent.Inventory() through --
	// unlike DescribeAreaMap/DescribeRegionalMap above, there is no
	// interactive per-item behavior yet (matching the mock's own "not
	// connected to the real engine yet" caveat), so this skips a
	// dedicated intermediate view-model type and produces the
	// presentation model directly, the same way MockSecondaryScreenContent
	// itself does.
	internal ModalViewModel DescribeInventory()
	{
		// Fully qualified: FiveEGoldBox.Application.Views.PartyViewModel
		// collides by simple name with this project's own (unrelated)
		// character-sidebar PartyViewModel in ui/models/viewmodels/.
		FiveEGoldBox.Application.Views.PartyViewModel party =
			SessionView.Describe(_state).Party;

		return new ModalViewModel(
			"Inventory",
			DescribePurse(party.Currency),
			party.InventoryItems
				.Select(item => new CommandViewModel(
					item.ItemId,
					$"{item.DisplayName} x{item.Quantity}"))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") });
	}

	// The scenario's own authored ending, surfaced as a real modal instead
	// of the one templated sentence DescribeStatus produces for the
	// message log behind it. Mirrors DescribeInventory/DescribeCharacter's
	// shape exactly -- one SessionView.Describe call, no new lookup.
	internal ModalViewModel DescribeConclusion()
	{
		SessionViewModel view = SessionView.Describe(_state);

		return new ModalViewModel(
			view.IsSuccess == true ? "Victory" : "Defeat",
			view.ConclusionText,
			Commands: new[] { new CommandViewModel("close", "Close", "C") });
	}

	private static string DescribePurse(CurrencyViewModel currency)
	{
		List<string> denominations = new();

		if (currency.PlatinumPieces != 0)
		{
			denominations.Add($"{currency.PlatinumPieces} pp");
		}

		if (currency.GoldPieces != 0)
		{
			denominations.Add($"{currency.GoldPieces} gp");
		}

		if (currency.ElectrumPieces != 0)
		{
			denominations.Add($"{currency.ElectrumPieces} ep");
		}

		if (currency.SilverPieces != 0)
		{
			denominations.Add($"{currency.SilverPieces} sp");
		}

		if (currency.CopperPieces != 0)
		{
			denominations.Add($"{currency.CopperPieces} cp");
		}

		return denominations.Count == 0
			? "The party's purse is empty."
			: $"Purse: {string.Join(", ", denominations)}.";
	}

	// A read-only party roster, the same shape ShowCharacterScreen's mock
	// content already renders through -- one row per member, body text
	// updated per selection via DescribeCharacterMember (mirrors the mock's
	// own onRowFocused convention).
	internal ModalViewModel DescribeCharacter()
	{
		FiveEGoldBox.Application.Views.PartyViewModel party =
			SessionView.Describe(_state).Party;
		string firstMemberId = party.Members[0].PartyMemberId;

		return new ModalViewModel(
			"Character",
			DescribeCharacterMember(firstMemberId),
			party.Members
				.Select(member => new CommandViewModel(
					member.PartyMemberId,
					$"{member.DisplayName} ({member.ClassDisplayName})"))
				.ToArray(),
			new[] { new CommandViewModel("close", "Close", "C") },
			SelectedRowId: firstMemberId);
	}

	internal string DescribeCharacterMember(string partyMemberId)
	{
		FiveEGoldBox.Application.Views.PartyViewModel party =
			SessionView.Describe(_state).Party;
		FiveEGoldBox.Application.Views.PartyMemberViewModel member = party
			.Members
			.First(candidate => candidate.PartyMemberId == partyMemberId);

		return $"{member.DisplayName} — {member.ClassDisplayName}, " +
			$"{member.CurrentHitPoints}/{member.MaximumHitPoints} HP.";
	}

	internal string Submit(string commandId)
	{
		if (!_lastActions.TryGetValue(commandId, out SessionAction? action))
		{
			throw new InvalidOperationException(
				$"Command '{commandId}' is not part of the last described " +
					"real-session snapshot.");
		}

		return action.Kind switch
		{
			SessionActionKind.ResolveOutpostDecision =>
				ResolveDecision(action),
			SessionActionKind.BeginJourney => BeginJourney(action),
			SessionActionKind.AdvanceTravel => AdvanceTravel(),
			SessionActionKind.EnterDestination => EnterDestination(),
			SessionActionKind.MoveForward => MoveForward(),
			SessionActionKind.TurnLeft =>
				Turn(ExplorationTurnDirection.Left, "Turned left."),
			SessionActionKind.TurnRight =>
				Turn(ExplorationTurnDirection.Right, "Turned right."),
			SessionActionKind.UseStairs => UseStairs(),
			SessionActionKind.ActivateTrigger => ActivateTrigger(action),
			SessionActionKind.OpenDoor => OpenDoor(),
			SessionActionKind.RevealSecretDoor => RevealSecretDoor(),
			SessionActionKind.CollectTreasure => CollectTreasure(),
			SessionActionKind.TalkToNpc => TalkToNpc(),
			SessionActionKind.PurchaseShopItem => PurchaseShopItem(action),
			_ => $"'{action.DisplayName}' is not connected to the real " +
				"engine yet.",
		};
	}

	// Godot's DirectMovement UX (M4) drives movement by arrow key rather
	// than a command-bar button per action — ShellInteractionController
	// collapses MoveForward/TurnLeft/TurnRight into one "Move" command that
	// enters that mode, then routes each keypress here directly rather
	// than through the by-ID Submit dispatch above, since the transient
	// "real.N" command IDs are only valid against the snapshot they came
	// from and movement mode deliberately never re-renders the command bar
	// between steps.
	//
	// MoveBackward exists as a Godot input action (numpad 2) but the real
	// engine has no such verb — ExplorationRules only ever moves forward
	// or turns, matching 5e Gold Box's own "turn to face a direction, walk
	// into it" convention. Shown honestly rather than silently ignored.
	internal string SubmitMovement(string movementCommandId)
	{
		return movementCommandId switch
		{
			ExplorationMovementCommandIds.MoveForward => MoveForward(),
			ExplorationMovementCommandIds.TurnLeft =>
				Turn(ExplorationTurnDirection.Left, "Turned left."),
			ExplorationMovementCommandIds.TurnRight =>
				Turn(ExplorationTurnDirection.Right, "Turned right."),
			ExplorationMovementCommandIds.MoveBackward =>
				"Backward movement is not offered by the real engine — " +
					"turn and walk forward instead.",
			_ => $"'{movementCommandId}' is not a recognized movement " +
				"command.",
		};
	}

	private string ResolveDecision(SessionAction action)
	{
		OutpostDecisionResult result = OutpostDecisionRules.Resolve(
			_state,
			action.DecisionOptionId
				?? throw new InvalidOperationException(
					"A ResolveOutpostDecision action had no option ID."));

		_state = result.State;

		return $"Choice made: {action.DisplayName}.";
	}

	private string BeginJourney(SessionAction action)
	{
		_travelOriginDisplayName = SessionView.Describe(_state).LocationDisplayName;
		_travelDestinationDisplayName = action.DestinationDisplayName;

		_state = RegionalTravelRules.BeginJourney(_state, action.RouteId);

		return "Journey begun.";
	}

	private string AdvanceTravel()
	{
		RegionalTravelAdvanceResult result =
			RegionalTravelRules.Advance(_state);

		_state = result.State;

		return result.DidArrive ? "Destination reached." : "Travel advanced.";
	}

	private string EnterDestination()
	{
		_state = ExplorationRules.EnterDestination(_state);

		return $"Entered {SessionView.Describe(_state).LocationDisplayName}.";
	}

	private string MoveForward()
	{
		ExplorationMoveResult result = ExplorationRules.MoveForward(_state);

		_state = result.State;

		return result.DidMove ? "Moved forward." : "Movement blocked.";
	}

	private string Turn(ExplorationTurnDirection direction, string message)
	{
		_state = ExplorationRules.Turn(_state, direction);

		return message;
	}

	private string UseStairs()
	{
		_state = ExplorationRules.UseStairs(_state);

		return "Used stairs.";
	}

	private string ActivateTrigger(SessionAction action)
	{
		_state = ScenarioTriggerRules.Activate(_state);

		return $"{action.DisplayName}.";
	}

	private string OpenDoor()
	{
		_state = ExplorationRules.OpenDoor(_state);

		return "Door opened.";
	}

	private string RevealSecretDoor()
	{
		_state = ExplorationRules.RevealSecretDoor(_state);

		return "A hidden door reveals itself.";
	}

	private string CollectTreasure()
	{
		_state = ExplorationRules.CollectTreasure(_state);

		return "Treasure collected.";
	}

	// Talking has no session-state consequence, so unlike every other
	// command here this doesn't reassign _state -- the returned line is
	// the whole result.
	private string TalkToNpc()
	{
		return ExplorationRules.DescribeNpcDialogue(_state);
	}

	private string PurchaseShopItem(SessionAction action)
	{
		_state = ShopRules.Purchase(
			_state,
			action.ShopItemId
				?? throw new InvalidOperationException(
					"A PurchaseShopItem action had no item ID."));

		return $"{action.DisplayName}.";
	}

	private static string DescribeStatus(SessionViewModel view)
	{
		if (view.IsConcluded)
		{
			return view.IsSuccess == true
				? $"{view.ScenarioDisplayName} concludes in victory."
				: $"{view.ScenarioDisplayName} concludes in defeat.";
		}

		return $"{view.ScenarioDisplayName} — {view.LocationDisplayName}.";
	}
}

internal sealed record RealSessionSnapshot(
	ApplicationMode Mode,
	string LocationDisplayName,
	string ModeLabel,
	string StatusMessage,
	bool IsConcluded,
	bool? IsSuccess,
	CommandSetViewModel Commands,
	RegionalMapViewModel? Map = null);
