using System;
using Godot;

internal sealed class ShellInputRouter
{
	private readonly IShellInteractionState _interactionState;
	private readonly Action _toggleImmersiveMode;
	private readonly Action _showExplorationView;
	private readonly Action _showRegionalMapView;
	private readonly Action _showCombatView;
	private readonly Action _toggleHighContrastTheme;
	private readonly Action _toggleReducedMotion;
	private readonly Action _exitExplorationMovementMode;
	private readonly Action<string> _reportMovement;
	private readonly Action _showNextMockScenario;
	private readonly Action _showPreviousMockScenario;
	private readonly Action _cycleResolutionPreset;

	public ShellInputRouter(
		IShellInteractionState interactionState,
		Action toggleImmersiveMode,
		Action showExplorationView,
		Action showRegionalMapView,
		Action showCombatView,
		Action toggleHighContrastTheme,
		Action toggleReducedMotion,
		Action exitExplorationMovementMode,
		Action<string> reportMovement,
		Action showNextMockScenario,
		Action showPreviousMockScenario,
		Action cycleResolutionPreset)
	{
		_interactionState = interactionState;
		_toggleImmersiveMode = toggleImmersiveMode;
		_showExplorationView = showExplorationView;
		_showRegionalMapView = showRegionalMapView;
		_showCombatView = showCombatView;
		_toggleHighContrastTheme = toggleHighContrastTheme;
		_toggleReducedMotion = toggleReducedMotion;
		_exitExplorationMovementMode = exitExplorationMovementMode;
		_reportMovement = reportMovement;
		_showNextMockScenario = showNextMockScenario;
		_showPreviousMockScenario = showPreviousMockScenario;
		_cycleResolutionPreset = cycleResolutionPreset;
	}

	// Takes the base InputEvent, not InputEventKey, so the same router
	// serves both AppShell._UnhandledKeyInput and its joypad-only
	// _UnhandledInput sibling — IsActionPressed resolves against whatever
	// PlayerInputActions bound the action to, keyboard or joypad alike.
	public bool Handle(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(
			PlayerInputActions.ToggleImmersiveMode))
		{
			_toggleImmersiveMode();
			return true;
		}

		if (HandleDeveloperShortcut(inputEvent))
		{
			return true;
		}

		return _interactionState.CurrentContext ==
				ShellInteractionContext.DirectMovement &&
			HandleExplorationMovementInput(inputEvent);
	}

	private bool HandleDeveloperShortcut(InputEvent inputEvent)
	{
#if DEBUG
		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevToggleHighContrastTheme))
		{
			_toggleHighContrastTheme();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevToggleReducedMotion))
		{
			_toggleReducedMotion();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevShowExplorationView))
		{
			_showExplorationView();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevShowRegionalMapView))
		{
			_showRegionalMapView();
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.DevShowCombatView))
		{
			_showCombatView();
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.DevNextMockScenario))
		{
			_showNextMockScenario();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevPreviousMockScenario))
		{
			_showPreviousMockScenario();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevCycleResolutionPreset))
		{
			_cycleResolutionPreset();
			return true;
		}

		return false;
#else
		return false;
#endif
	}

	private bool HandleExplorationMovementInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(PlayerInputActions.UiCancel) ||
			inputEvent.IsActionPressed(PlayerInputActions.ExitMovementMode))
		{
			_exitExplorationMovementMode();
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.MoveForward))
		{
			_reportMovement("Step forward");
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.MoveBackward))
		{
			_reportMovement("Step backward");
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.TurnLeft))
		{
			_reportMovement("Turn left");
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.TurnRight))
		{
			_reportMovement("Turn right");
			return true;
		}

		return false;
	}
}
