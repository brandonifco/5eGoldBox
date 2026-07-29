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
		_presentationController.SetExplorationMovementOverlayActive(true);
		_presentationController.SetMessage(
			"Movement active: arrows or numpad 8/2 move; " +
			"4/6 turn. Press Esc or Space to return.");
	}
}
