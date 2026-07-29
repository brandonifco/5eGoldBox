using System.Collections.Generic;
using FiveEGoldBox.Application.Sessions;
using Godot;

// The real integration seam's own command/movement handling — split out
// of ShellInteractionController.cs (see that file's header) once this
// pushed the combined class past the governing plan's 250-line review
// threshold. Mirrors .Exploration.cs/.RegionalMap.cs/.Combat.cs's role
// for their own modes.
internal sealed partial class ShellInteractionController
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

	private RealGameSession? _activeRealMovementSession;

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
}
