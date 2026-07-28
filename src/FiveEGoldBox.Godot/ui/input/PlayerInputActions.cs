using Godot;

// Registered in code rather than project.godot so every binding is a
// compiler-checked Key value instead of a hand-authored resource block.
internal static class PlayerInputActions
{
	public const string UiCancel = "ui_cancel";
	public const string UiUp = "ui_up";
	public const string UiDown = "ui_down";
	public const string UiFocusNext = "ui_focus_next";
	public const string UiFocusPrev = "ui_focus_prev";

	public const string ToggleImmersiveMode = "shell_toggle_immersive_mode";
	public const string ExitMovementMode = "shell_exit_movement_mode";
	public const string MoveForward = "shell_move_forward";
	public const string MoveBackward = "shell_move_backward";
	public const string TurnLeft = "shell_turn_left";
	public const string TurnRight = "shell_turn_right";
	public const string ListSelectFirst = "shell_list_select_first";
	public const string ListSelectLast = "shell_list_select_last";
	public const string DevShowExplorationView =
		"shell_dev_show_exploration_view";
	public const string DevShowRegionalMapView =
		"shell_dev_show_regional_map_view";
	public const string DevShowCombatView = "shell_dev_show_combat_view";
	public const string DevToggleHighContrastTheme =
		"shell_dev_toggle_high_contrast_theme";
	public const string DevToggleReducedMotion =
		"shell_dev_toggle_reduced_motion";

	public static void EnsureRegistered()
	{
		Register(ToggleImmersiveMode, new InputEventKey { Keycode = Key.F11 });

		Register(ExitMovementMode, new InputEventKey { Keycode = Key.Space });

		Register(
			MoveForward,
			new InputEventKey { Keycode = Key.Up },
			new InputEventKey { Keycode = Key.Kp8 });
		Register(
			MoveBackward,
			new InputEventKey { Keycode = Key.Down },
			new InputEventKey { Keycode = Key.Kp2 });
		Register(
			TurnLeft,
			new InputEventKey { Keycode = Key.Left },
			new InputEventKey { Keycode = Key.Kp4 });
		Register(
			TurnRight,
			new InputEventKey { Keycode = Key.Right },
			new InputEventKey { Keycode = Key.Kp6 });

		Register(ListSelectFirst, new InputEventKey { Keycode = Key.Home });
		Register(ListSelectLast, new InputEventKey { Keycode = Key.End });

		Register(
			DevShowExplorationView,
			new InputEventKey { Keycode = Key.F1 });
		Register(
			DevShowRegionalMapView,
			new InputEventKey { Keycode = Key.F2 });
		Register(DevShowCombatView, new InputEventKey { Keycode = Key.F3 });

		Register(
			DevToggleHighContrastTheme,
			new InputEventKey
			{
				Keycode = Key.H,
				CtrlPressed = true,
				AltPressed = true,
			});
		Register(
			DevToggleReducedMotion,
			new InputEventKey
			{
				Keycode = Key.M,
				CtrlPressed = true,
				AltPressed = true,
			});
	}

	private static void Register(string action, params InputEventKey[] events)
	{
		if (InputMap.HasAction(action))
		{
			return;
		}

		InputMap.AddAction(action);

		foreach (InputEventKey inputEvent in events)
		{
			InputMap.ActionAddEvent(action, inputEvent);
		}
	}
}
