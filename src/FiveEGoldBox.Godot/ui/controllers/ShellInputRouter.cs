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

	public bool Handle(
		Key keycode,
		bool ctrlPressed,
		bool altPressed)
	{
		if (keycode == Key.F11)
		{
			_toggleImmersiveMode();
			return true;
		}

		if (HandleDeveloperShortcut(
			keycode,
			ctrlPressed,
			altPressed))
		{
			return true;
		}

		return _interactionState.CurrentMode ==
				InteractionMode.ExplorationMovement &&
			HandleExplorationMovementInput(keycode);
	}

	private bool HandleDeveloperShortcut(
		Key keycode,
		bool ctrlPressed,
		bool altPressed)
	{
#if DEBUG
		if (ctrlPressed && altPressed)
		{
			switch (keycode)
			{
				case Key.H:
					_toggleHighContrastTheme();
					return true;

				case Key.M:
					_toggleReducedMotion();
					return true;
			}
		}

		switch (keycode)
		{
			case Key.F1:
				_showExplorationView();
				return true;

			case Key.F2:
				_showRegionalMapView();
				return true;

			case Key.F3:
				_showCombatView();
				return true;

			default:
				return false;
		}
#else
		return false;
#endif
	}

	private bool HandleExplorationMovementInput(Key keycode)
	{
		switch (keycode)
		{
			case Key.Escape:
			case Key.Space:
				_exitExplorationMovementMode();
				return true;

			case Key.Up:
			case Key.Kp8:
				_reportMovement("Step forward");
				return true;

			case Key.Down:
			case Key.Kp2:
				_reportMovement("Step backward");
				return true;

			case Key.Left:
			case Key.Kp4:
				_reportMovement("Turn left");
				return true;

			case Key.Right:
			case Key.Kp6:
				_reportMovement("Turn right");
				return true;

			default:
				return false;
		}
	}
}
