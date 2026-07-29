using System;
using System.Collections.Generic;

// Exploration command shells (View/Cast/Area/Encamp/Search/Look) and
// movement-mode handling — split out of ShellInteractionController.cs
// (see that file's header) once the combined class crossed the governing
// plan's 250-line review threshold.
internal sealed partial class ShellInteractionController
{
	// M6d: takes the real UiCommandIntent a keypress now constructs
	// (ShellInputRouter) instead of a raw display string — the first real
	// player action to reach a UiCommandIntent, not just mock content.
	// M6f: turning now actually rotates the visible facing/compass badges
	// instead of only ever changing the message text — there is no grid
	// position yet, so forward/backward still has nothing to move.
	//
	// Real-session movement (PR #171, ShellInteractionController.
	// RealSession.cs's EnterRealMovementMode) is routed to
	// RealGameSession.SubmitMovement directly rather than the by-ID
	// Submit dispatch other commands use — movement mode deliberately
	// never re-renders the command bar between steps, so there is no
	// fresh snapshot to look a transient command ID up in. Turning still
	// updates the facing badge either way — that capability postdates
	// PR #171 and applies just as well to a real session as a mock one.
	public void ReportMovement(UiCommandIntent intent)
	{
		if (intent.CommandId == ExplorationMovementCommandIds.TurnLeft)
		{
			_presentationController.TurnFacing(turnLeft: true);
		}
		else if (intent.CommandId == ExplorationMovementCommandIds.TurnRight)
		{
			_presentationController.TurnFacing(turnLeft: false);
		}

		if (_activeRealMovementSession is not null)
		{
			string message =
				_activeRealMovementSession.SubmitMovement(intent.CommandId);
			RealSessionSnapshot snapshot = _activeRealMovementSession.Describe();

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
	// CommandDefinition construction — RegionalMap/Combat stay hardcoded
	// on purpose, that's M7/M8's own milestone to do the same.
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
			// M7e: the deterministic return trip — reuses the same public
			// ShowRegionalMap() entry point F2/dev and the regional map's
			// own "Enter" command use, so there is exactly one way this
			// transition happens, not two.
			["exit"] = ShowRegionalMap,
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
}
