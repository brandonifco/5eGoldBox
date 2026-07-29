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
// Combat and scenario conclusion are detected and shown honestly rather
// than faked — see ShellInteractionController.ShowRealSession.
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
				AssignHotkey(action.DisplayName, usedHotkeys)));
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
	// named points on a line.
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

		return new RegionalMapViewModel(
			RegionalMapKey,
			new RegionalMapMarkerViewModel(
				"party",
				"Party",
				new RegionalMapPointViewModel(progress * 100.0, 0)),
			new[]
			{
				new RegionalMapMarkerViewModel(
					travel.OriginLocationId,
					originLabel,
					new RegionalMapPointViewModel(0, 0)),
				new RegionalMapMarkerViewModel(
					travel.DestinationLocationId,
					destinationLabel,
					new RegionalMapPointViewModel(100, 0),
					Selected: travel.IsComplete),
			});
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

	private const string SetOutForPrefix = "Set out for ";

	private string BeginJourney(SessionAction action)
	{
		_travelOriginDisplayName = SessionView.Describe(_state).LocationDisplayName;
		_travelDestinationDisplayName = action.DisplayName.StartsWith(
			SetOutForPrefix,
			StringComparison.Ordinal)
			? action.DisplayName[SetOutForPrefix.Length..]
			: action.DisplayName;

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

	// Godot's ShowCommands throws on a duplicate ShortcutKey within one
	// call, so real content (arbitrary DisplayName strings, unlike the
	// hand-picked mock command letters) needs a collision-free assignment
	// rather than always taking the first letter.
	private static string AssignHotkey(
		string displayName,
		HashSet<char> usedHotkeys)
	{
		foreach (char candidate in displayName
			.Where(char.IsLetter)
			.Select(char.ToUpperInvariant))
		{
			if (usedHotkeys.Add(candidate))
			{
				return candidate.ToString();
			}
		}

		for (char digit = '1'; digit <= '9'; digit++)
		{
			if (usedHotkeys.Add(digit))
			{
				return digit.ToString();
			}
		}

		throw new InvalidOperationException(
			$"Could not assign a hotkey for '{displayName}'; every " +
				"letter and digit is already taken in this command set.");
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
