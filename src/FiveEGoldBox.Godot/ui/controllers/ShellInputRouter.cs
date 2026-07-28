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

	public ShellInputRouter(
		IShellInteractionState interactionState,
		Action toggleImmersiveMode,
		Action showExplorationView,
		Action showRegionalMapView,
		Action showCombatView,
		Action toggleHighContrastTheme,
		Action toggleReducedMotion,
		Action exitExplorationMovementMode,
		Action<string> reportMovement)
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
	}

	public bool Handle(InputEventKey keyEvent)
	{
		if (keyEvent.IsActionPressed(PlayerInputActions.ToggleImmersiveMode))
		{
			_toggleImmersiveMode();
			return true;
		}

		if (HandleDeveloperShortcut(keyEvent))
		{
			return true;
		}

		return _interactionState.CurrentContext ==
				ShellInteractionContext.ExplorationMovement &&
			HandleExplorationMovementInput(keyEvent);
	}

	private bool HandleDeveloperShortcut(InputEventKey keyEvent)
	{
#if DEBUG
		if (keyEvent.IsActionPressed(
			PlayerInputActions.DevToggleHighContrastTheme))
		{
			_toggleHighContrastTheme();
			return true;
		}

		if (keyEvent.IsActionPressed(
			PlayerInputActions.DevToggleReducedMotion))
		{
			_toggleReducedMotion();
			return true;
		}

		if (keyEvent.IsActionPressed(
			PlayerInputActions.DevShowExplorationView))
		{
			_showExplorationView();
			return true;
		}

		if (keyEvent.IsActionPressed(
			PlayerInputActions.DevShowRegionalMapView))
		{
			_showRegionalMapView();
			return true;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.DevShowCombatView))
		{
			_showCombatView();
			return true;
		}

		return false;
#else
		return false;
#endif
	}

	private bool HandleExplorationMovementInput(InputEventKey keyEvent)
	{
		if (keyEvent.IsActionPressed(PlayerInputActions.UiCancel) ||
			keyEvent.IsActionPressed(PlayerInputActions.ExitMovementMode))
		{
			_exitExplorationMovementMode();
			return true;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.MoveForward))
		{
			_reportMovement("Step forward");
			return true;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.MoveBackward))
		{
			_reportMovement("Step backward");
			return true;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.TurnLeft))
		{
			_reportMovement("Turn left");
			return true;
		}

		if (keyEvent.IsActionPressed(PlayerInputActions.TurnRight))
		{
			_reportMovement("Turn right");
			return true;
		}

		return false;
	}
}
