using System;
using System.Collections.Generic;
using FiveEGoldBox.Application.Sessions;
using Godot;

internal sealed class ShellInteractionController : IShellInteractionState
{
	// SessionView's own literal DisplayName strings for the three movement
	// actions it always offers unconditionally in Exploration mode — the
	// seam that lets ShowRealCommands collapse them into one "Move" entry
	// driving the existing DirectMovement UX (M4) instead of three flat
	// buttons. No Kind is available at this layer (RealGameSession already
	// projected SessionAction into CommandViewModel by here), so this
	// matches by the same stable content strings the player sees.
	private const string MoveForwardLabel = "Move forward";
	private const string TurnLeftLabel = "Turn left";
	private const string TurnRightLabel = "Turn right";

	private readonly IShellPresentation _presentationController;
	private readonly IShellCommandBar _commandBarController;
	private readonly IShellConfirmation _confirmation;
	private readonly Stack<ShellInteractionContext> _contextStack = new();
	private RealGameSession? _activeRealMovementSession;

	public ShellInteractionController(
		IShellPresentation presentationController,
		IShellCommandBar commandBarController,
		IShellConfirmation confirmation)
	{
		_presentationController = presentationController;
		_commandBarController = commandBarController;
		_confirmation = confirmation;
		_contextStack.Push(ShellInteractionContext.CommandMenu);
	}

	public ShellInteractionContext CurrentContext => _contextStack.Peek();

	public void ShowExploration()
	{
		ResetContext();
		_presentationController.ShowExploration();
		ShowExplorationCommands();
	}

	// The real integration seam. Renders whatever RealGameSession.Describe()
	// reports, rather than the mock content ShowExploration()/ShowCombat()
	// otherwise fall back to. overrideMessage carries a just-submitted
	// command's real result text (e.g. "Moved forward.") so re-rendering
	// after an action doesn't immediately clobber it with the generic
	// status line.
	//
	// Combat and scenario conclusion are shown honestly rather than faked:
	// this scope is outpost decisions, exploration, and regional travel
	// only (see RealGameSession's own header comment for why).
	public void ShowRealSession(
		RealGameSession session,
		string? overrideMessage = null)
	{
		ResetContext();

		RealSessionSnapshot snapshot = session.Describe();

		switch (snapshot.Mode)
		{
			case ApplicationMode.Encounter:
				ShowCombat();
				_presentationController.SetHeader(
					snapshot.LocationDisplayName,
					"Combat");
				_presentationController.SetMessage(
					overrideMessage
						?? "A fight has begun. Combat is not connected " +
							"to the real engine yet.");
				return;
			case ApplicationMode.ScenarioConclusion:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.OutpostEntrance,
					snapshot.LocationDisplayName,
					"Conclusion",
					overrideMessage ?? snapshot.StatusMessage);
				_commandBarController.ShowCommands();
				return;
			case ApplicationMode.RegionalTravel:
				_presentationController.ShowRegionalMap();

				if (snapshot.Map is not null)
				{
					_presentationController.ConfigureRegionalMap(
						snapshot.Map);
				}

				_presentationController.SetHeader(
					snapshot.LocationDisplayName,
					snapshot.ModeLabel);
				_presentationController.SetMessage(
					overrideMessage ?? snapshot.StatusMessage);
				break;
			case ApplicationMode.Outpost:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.OutpostEntrance,
					snapshot.LocationDisplayName,
					snapshot.ModeLabel,
					overrideMessage ?? snapshot.StatusMessage);
				break;
			default:
				_presentationController.ShowExploration(
					ExplorationSceneKeys.DungeonCorridor,
					snapshot.LocationDisplayName,
					snapshot.ModeLabel,
					overrideMessage ?? snapshot.StatusMessage);
				break;
		}

		ShowRealCommands(session, snapshot);
	}

	private void ShowRealCommands(
		RealGameSession session,
		RealSessionSnapshot snapshot)
	{
		List<CommandDefinition> commands = new();
		bool offersMovement = false;

		foreach (CommandViewModel commandViewModel in
			snapshot.Commands.Commands)
		{
			if (commandViewModel.Label is MoveForwardLabel
				or TurnLeftLabel
				or TurnRightLabel)
			{
				offersMovement = true;
				continue;
			}

			string commandId = commandViewModel.CommandId;

			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				() => SubmitRealCommand(session, commandId)));
		}

		if (offersMovement)
		{
			commands.Insert(
				0,
				new CommandDefinition(
					"Move",
					"[b]M[/b]ove",
					Key.M,
					() => EnterRealMovementMode(session)));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	private void SubmitRealCommand(RealGameSession session, string commandId)
	{
		string message = session.Submit(commandId);

		ShowRealSession(session, message);
	}

	private void EnterRealMovementMode(RealGameSession session)
	{
		_activeRealMovementSession = session;
		PushContext(ShellInteractionContext.DirectMovement);

		_commandBarController.ShowMovementPrompt();
		_presentationController.SetMessage(
			"Movement active: arrows or numpad 8 move forward; " +
			"4/6 turn. Press Esc or Space to return.");
	}

	public void ShowRegionalMap()
	{
		ResetContext();
		_presentationController.ShowRegionalMap();

		_commandBarController.ShowCommands(
			new CommandDefinition(
				"Travel",
				"[b]T[/b]ravel",
				Key.T,
				() => ReportCommand("Travel")),
			new CommandDefinition(
				"Search",
				"[b]S[/b]earch",
				Key.S,
				() => ReportCommand("Search")),
			new CommandDefinition(
				"Enter",
				"[b]E[/b]nter",
				Key.E,
				() => ReportCommand("Enter")),
			new CommandDefinition(
				"Camp",
				"[b]C[/b]amp",
				Key.C,
				() => ReportCommand("Camp")),
			new CommandDefinition(
				"Inventory",
				"[b]I[/b]nventory",
				Key.I,
				() => ReportCommand("Inventory")),
			new CommandDefinition(
				"Journal",
				"[b]J[/b]ournal",
				Key.J,
				() => ReportCommand("Journal")));
	}

	public void ShowCombat()
	{
		ResetContext();
		_presentationController.ShowCombat();

		_commandBarController.ShowCommands(
			new CommandDefinition(
				"Move",
				"[b]M[/b]ove",
				Key.M,
				() => ReportCommand("Combat movement")),
			new CommandDefinition(
				"Attack",
				"[b]A[/b]ttack",
				Key.A,
				() => ReportCommand("Attack")),
			new CommandDefinition(
				"Cast",
				"[b]C[/b]ast",
				Key.C,
				() => ReportCommand("Cast")),
			new CommandDefinition(
				"Use",
				"[b]U[/b]se",
				Key.U,
				() => ReportCommand("Use")),
			new CommandDefinition(
				"Defend",
				"[b]D[/b]efend",
				Key.D,
				() => ReportCommand("Defend")),
			new CommandDefinition(
				"End Turn",
				"[b]E[/b]nd Turn",
				Key.E,
				() => ReportCommand("End turn")));
	}

	public void ExitExplorationMovementMode()
	{
		PopContext(ShellInteractionContext.DirectMovement);

		if (_activeRealMovementSession is not null)
		{
			RealGameSession session = _activeRealMovementSession;

			_activeRealMovementSession = null;
			ShowRealSession(session, "Movement mode ended.");
			return;
		}

		_presentationController.SetMessage("Movement mode ended.");
		ShowExplorationCommands();
	}

	// M6d: takes the real UiCommandIntent a keypress now constructs
	// (ShellInputRouter) instead of a raw display string. Routed to
	// RealGameSession.SubmitMovement directly rather than through the
	// by-ID Submit dispatch other commands use — movement mode
	// deliberately never re-renders the command bar between steps, so
	// there is no fresh snapshot to look a transient command ID up in.
	public void ReportMovement(UiCommandIntent intent)
	{
		if (_activeRealMovementSession is not null)
		{
			string message =
				_activeRealMovementSession.SubmitMovement(intent.CommandId);
			RealSessionSnapshot snapshot =
				_activeRealMovementSession.Describe();

			_presentationController.SetHeader(
				snapshot.LocationDisplayName,
				snapshot.ModeLabel);
			_presentationController.SetMessage(message);
			return;
		}

		_presentationController.SetMessage(
			$"{DescribeMovement(intent.CommandId)}. " +
				"Backend movement is not connected yet.");
	}

	private static string DescribeMovement(string commandId)
	{
		return commandId switch
		{
			ExplorationMovementCommandIds.MoveForward => "Step forward",
			ExplorationMovementCommandIds.MoveBackward => "Step backward",
			ExplorationMovementCommandIds.TurnLeft => "Turn left",
			ExplorationMovementCommandIds.TurnRight => "Turn right",
			_ => commandId,
		};
	}

	// M6c: driven from CommandSetViewModel data instead of hardcoded
	// CommandDefinition construction — RegionalMap/Combat above stay
	// hardcoded on purpose, that's M7/M8's own milestone to do the same.
	private void ShowExplorationCommands()
	{
		CommandSetViewModel commandSet = MockExplorationCommandContent.Current();
		Dictionary<string, Action> handlers =
			BuildExplorationCommandHandlers();
		List<CommandDefinition> commands = new();

		foreach (CommandViewModel commandViewModel in commandSet.Commands)
		{
			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				handlers[commandViewModel.CommandId]));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	// M6e: each shell is deliberately distinct rather than one generic
	// "X selected" template — Encamp is a real decision (ConfirmationDialog,
	// M3's component, first live use outside dev demos), the other five
	// are one-line placeholder results. Full versions (a real Character
	// view, Spellbook, Area map) are M9's job; these are shells, not
	// screens.
	private Dictionary<string, Action> BuildExplorationCommandHandlers()
	{
		return new Dictionary<string, Action>
		{
			["move"] = EnterExplorationMovementMode,
			["view"] = () => _presentationController.SetMessage(
				"You check the party's status. " +
					"Detailed character views are not connected yet."),
			["cast"] = () => _presentationController.SetMessage(
				"You have no spells ready. " +
					"Spellcasting is not connected yet."),
			["area"] = () => _presentationController.SetMessage(
				"You get your bearings. " +
					"Area details are not connected yet."),
			["encamp"] = ShowEncampConfirmation,
			["search"] = () => _presentationController.SetMessage(
				"You search the area but find nothing. " +
					"Search mechanics are not connected yet."),
			["look"] = () => _presentationController.SetMessage(
				"You look around. " +
					"Descriptive detail is not connected yet."),
		};
	}

	private void ShowEncampConfirmation()
	{
		PushContext(ShellInteractionContext.Confirmation);

		_confirmation.ShowConfirmation(
			"Encamp",
			"Rest here and recover?",
			"Rest",
			"Cancel",
			onConfirmed: () => _presentationController.SetMessage(
				"You make camp and rest. " +
					"Rest mechanics are not connected yet."),
			onClosed: () => PopContext(ShellInteractionContext.Confirmation));
	}

	private void EnterExplorationMovementMode()
	{
		PushContext(ShellInteractionContext.DirectMovement);

		_commandBarController.ShowMovementPrompt();
		_presentationController.SetMessage(
			"Movement active: arrows or numpad 8/2 move; " +
			"4/6 turn. Press Esc or Space to return.");
	}

	private void ReportCommand(string command)
	{
		_presentationController.SetMessage(
			$"{command} selected. Backend behavior is not connected yet.");
	}

	private void ResetContext()
	{
		_contextStack.Clear();
		_contextStack.Push(ShellInteractionContext.CommandMenu);
	}

	// Cancel priority is Godot's own unhandled-input order (deepest node
	// first, SetInputAsHandled stops it going further) — a modal already
	// wins over ShellInputRouter's exploration-movement Escape handling
	// for that reason alone. This stack exists so CurrentContext reports
	// the same priority explicitly: whoever captures a cancel key this
	// way must Push/Pop their context here too, or CurrentContext goes
	// stale while they hold input.
	private void PushContext(ShellInteractionContext context)
	{
		_contextStack.Push(context);
	}

	private void PopContext(ShellInteractionContext expected)
	{
		if (CurrentContext == expected && _contextStack.Count > 1)
		{
			_contextStack.Pop();
		}
	}
}
