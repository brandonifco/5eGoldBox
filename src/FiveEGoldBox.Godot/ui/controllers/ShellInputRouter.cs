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
	private readonly Action<UiCommandIntent> _reportMovement;
	private readonly Action _showNextMockScenario;
	private readonly Action _showPreviousMockScenario;
	private readonly Action _cycleResolutionPreset;
	private readonly Action _advanceScriptedSession;
	private readonly Action _cycleExplorationVariant;
	private readonly Action<bool> _cycleRegionalMapZoom;
	private readonly Action _cancelCombatTargeting;

	public ShellInputRouter(
		IShellInteractionState interactionState,
		Action toggleImmersiveMode,
		Action showExplorationView,
		Action showRegionalMapView,
		Action showCombatView,
		Action toggleHighContrastTheme,
		Action toggleReducedMotion,
		Action exitExplorationMovementMode,
		Action<UiCommandIntent> reportMovement,
		Action showNextMockScenario,
		Action showPreviousMockScenario,
		Action cycleResolutionPreset,
		Action advanceScriptedSession,
		Action cycleExplorationVariant,
		Action<bool> cycleRegionalMapZoom,
		Action cancelCombatTargeting)
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
		_advanceScriptedSession = advanceScriptedSession;
		_cycleExplorationVariant = cycleExplorationVariant;
		_cycleRegionalMapZoom = cycleRegionalMapZoom;
		_cancelCombatTargeting = cancelCombatTargeting;
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

		if (_interactionState.CurrentContext ==
			ShellInteractionContext.DirectMovement)
		{
			return HandleExplorationMovementInput(inputEvent);
		}

		if (_interactionState.CurrentContext ==
			ShellInteractionContext.SelectionList)
		{
			return HandleRegionalMapInput(inputEvent);
		}

		if (_interactionState.CurrentContext ==
			ShellInteractionContext.Targeting)
		{
			return HandleCombatTargetingInput(inputEvent);
		}

		return false;
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

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevAdvanceScriptedSession))
		{
			_advanceScriptedSession();
			return true;
		}

		if (inputEvent.IsActionPressed(
			PlayerInputActions.DevCycleExplorationVariant))
		{
			_cycleExplorationVariant();
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
			_reportMovement(
				new UiCommandIntent(ExplorationMovementCommandIds.MoveForward));
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.MoveBackward))
		{
			_reportMovement(
				new UiCommandIntent(ExplorationMovementCommandIds.MoveBackward));
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.TurnLeft))
		{
			_reportMovement(
				new UiCommandIntent(ExplorationMovementCommandIds.TurnLeft));
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.TurnRight))
		{
			_reportMovement(
				new UiCommandIntent(ExplorationMovementCommandIds.TurnRight));
			return true;
		}

		return false;
	}

	// M7b: keyboard zoom, gated to the regional map's own default
	// context — mouse-wheel zoom is handled locally by RegionalMapView
	// itself instead, the same way a ScrollContainer handles its own
	// wheel input without going through this central router.
	private bool HandleRegionalMapInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(PlayerInputActions.RegionalMapZoomIn))
		{
			_cycleRegionalMapZoom(true);
			return true;
		}

		if (inputEvent.IsActionPressed(PlayerInputActions.RegionalMapZoomOut))
		{
			_cycleRegionalMapZoom(false);
			return true;
		}

		return false;
	}

	// M8e: cancel out of a pending Move/Attack/Cast/Use target selection
	// without resolving it — same UiCancel/ExitMovementMode keys as
	// exploration's own movement-mode cancel.
	private bool HandleCombatTargetingInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed(PlayerInputActions.UiCancel) ||
			inputEvent.IsActionPressed(PlayerInputActions.ExitMovementMode))
		{
			_cancelCombatTargeting();
			return true;
		}

		return false;
	}
}
