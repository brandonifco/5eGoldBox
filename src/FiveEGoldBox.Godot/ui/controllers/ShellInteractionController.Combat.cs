using System;
using System.Collections.Generic;

// Combat command shells and the targeting flow they open into — mirrors
// .Exploration.cs/.RegionalMap.cs's role for their own modes. M8d/e.
internal sealed partial class ShellInteractionController
{
	// Which command opened Targeting mode — null outside it. Read by the
	// two target-activation handlers below to know what a click/Enter on
	// a cell or combatant actually means.
	private string? _pendingCombatCommand;

	private void ShowCombatCommands()
	{
		CommandSetViewModel commandSet = MockCombatCommandContent.Current();
		Dictionary<string, Action> handlers = BuildCombatCommandHandlers();
		List<CommandDefinition> commands = new();

		foreach (CommandViewModel commandViewModel in commandSet.Commands)
		{
			commands.Add(CommandViewModelTranslator.ToCommandDefinition(
				commandViewModel,
				handlers[commandViewModel.CommandId]));
		}

		_commandBarController.ShowCommands(commands.ToArray());
	}

	private Dictionary<string, Action> BuildCombatCommandHandlers()
	{
		return new Dictionary<string, Action>
		{
			["move"] = () => EnterCombatTargeting(
				"move",
				MockCombatContent.MoveRangeHighlights(ActiveCombatantOrFallback())),
			["attack"] = () => EnterCombatTargeting(
				"attack",
				MockCombatContent.TargetHighlights(targetAllies: false)),
			["cast"] = () => EnterCombatTargeting(
				"cast",
				MockCombatContent.TargetHighlights(targetAllies: true)),
			["use"] = () => EnterCombatTargeting(
				"use",
				MockCombatContent.TargetHighlights(targetAllies: true)),
			["defend"] = () => _presentationController.SetMessage(
				"You brace to defend. Defend mechanics are not connected yet."),
			["end-turn"] = EndCombatTurn,
		};
	}

	// M8e: Targeting is this milestone's first real use of the context
	// M4b declared ahead of need ("M6/M8/M9's first... target picker").
	// Cancel (Esc/Space, gated on this context in ShellInputRouter) backs
	// out without resolving anything; Tab/Shift+Tab cycles between the
	// highlighted cells/combatants for free, since they are real
	// focusable Buttons (CombatHighlightCell/CombatantMarkerPin) — the
	// same "reuse Godot's own focus system" insight M7a used for
	// SelectionList, applied here without SelectionList itself.
	private void EnterCombatTargeting(
		string commandId,
		IReadOnlyList<CombatHighlightViewModel> highlights)
	{
		_pendingCombatCommand = commandId;
		PushContext(ShellInteractionContext.Targeting);
		_presentationController.ShowCombatHighlights(highlights);
		_presentationController.SetMessage(
			$"Choose a target for {DescribeCombatCommand(commandId)}. " +
				"Press Esc to cancel.");
	}

	public void CancelCombatTargeting()
	{
		PopContext(ShellInteractionContext.Targeting);
		_pendingCombatCommand = null;
		_presentationController.ShowCombatHighlights(null);
		_presentationController.SetMessage("Cancelled.");
	}

	private void OnCombatCellTargeted(int gridX, int gridY)
	{
		if (_pendingCombatCommand != "move")
		{
			return;
		}

		PopContext(ShellInteractionContext.Targeting);
		_pendingCombatCommand = null;
		_presentationController.ShowCombatHighlights(null);
		_presentationController.MoveActiveCombatant(gridX, gridY);
		_presentationController.SetMessage(
			"You move. Movement legality is not connected yet.");
	}

	private void OnCombatantTargeted(string combatantId)
	{
		if (_pendingCombatCommand is not string commandId)
		{
			return;
		}

		CombatantMarkerViewModel? target = MockCombatContent.Find(combatantId);

		if (target is null)
		{
			return;
		}

		PopContext(ShellInteractionContext.Targeting);
		_pendingCombatCommand = null;
		_presentationController.ShowCombatHighlights(null);

		if (commandId == "attack")
		{
			ShowAttackConfirmation(target);
			return;
		}

		_presentationController.SetMessage(
			$"{DescribeCombatCommand(commandId)} used on {target.Label}. " +
				"Effects are not connected yet.");
	}

	// M8e: the one combat command with real consequence gets a real
	// confirmation, same Encamp precedent M6e/M7d already established —
	// reuses the same ConfirmationDialog mechanism rather than a second
	// wiring of it.
	private void ShowAttackConfirmation(CombatantMarkerViewModel target)
	{
		PushContext(ShellInteractionContext.Confirmation);
		_presentationController.SelectCombatTarget(target.Id);

		_confirmation.ShowConfirmation(
			"Attack",
			$"Attack {target.Label}?",
			"Attack",
			"Cancel",
			onConfirmed: () => _presentationController.SetMessage(
				$"You attack {target.Label}. Damage is not connected yet."),
			onClosed: () =>
			{
				PopContext(ShellInteractionContext.Confirmation);
				_presentationController.SelectCombatTarget(null);
			});
	}

	private void EndCombatTurn()
	{
		_presentationController.AdvanceCombatTurn();

		string? label = _presentationController.ActiveCombatant?.Label;
		_presentationController.SetMessage(
			label is null ? "Turn ended." : $"Turn ended. {label} is up next.");
	}

	private CombatantMarkerViewModel ActiveCombatantOrFallback()
	{
		return _presentationController.ActiveCombatant ?? MockCombatContent.Combatants[0];
	}

	private static string DescribeCombatCommand(string commandId)
	{
		return commandId switch
		{
			"move" => "Move",
			"attack" => "Attack",
			"cast" => "Cast",
			"use" => "Use",
			_ => commandId,
		};
	}
}
