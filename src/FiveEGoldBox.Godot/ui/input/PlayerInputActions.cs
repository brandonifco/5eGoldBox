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
	public const string RegionalMapZoomIn = "shell_regional_map_zoom_in";
	public const string RegionalMapZoomOut = "shell_regional_map_zoom_out";
	public const string PartyCursorPrevious = "shell_party_cursor_previous";
	public const string PartyCursorNext = "shell_party_cursor_next";
	public const string DevShowExplorationView =
		"shell_dev_show_exploration_view";
	public const string DevShowRegionalMapView =
		"shell_dev_show_regional_map_view";
	public const string DevShowCombatView = "shell_dev_show_combat_view";
	public const string DevToggleHighContrastTheme =
		"shell_dev_toggle_high_contrast_theme";
	public const string DevToggleReducedMotion =
		"shell_dev_toggle_reduced_motion";
	public const string DevNextMockScenario = "shell_dev_next_mock_scenario";
	public const string DevPreviousMockScenario =
		"shell_dev_previous_mock_scenario";
	public const string DevCycleResolutionPreset =
		"shell_dev_cycle_resolution_preset";
	public const string DevAdvanceScriptedSession =
		"shell_dev_advance_scripted_session";
	public const string DevCycleExplorationVariant =
		"shell_dev_cycle_exploration_variant";

	public static void EnsureRegistered()
	{
		Register(ToggleImmersiveMode, new InputEventKey { Keycode = Key.F11 });

		Register(ExitMovementMode, new InputEventKey { Keycode = Key.Space });

		// D-pad only for now, deliberately — analog-stick movement needs
		// deadzone tuning and continuous-vs-discrete handling that would
		// be locking in real controller-binding decisions, not scaffolding
		// them. See M4f's milestone note before adding stick support.
		Register(
			MoveForward,
			new InputEventKey { Keycode = Key.Up },
			new InputEventKey { Keycode = Key.Kp8 },
			new InputEventJoypadButton { ButtonIndex = JoyButton.DpadUp });
		Register(
			MoveBackward,
			new InputEventKey { Keycode = Key.Down },
			new InputEventKey { Keycode = Key.Kp2 },
			new InputEventJoypadButton { ButtonIndex = JoyButton.DpadDown });
		Register(
			TurnLeft,
			new InputEventKey { Keycode = Key.Left },
			new InputEventKey { Keycode = Key.Kp4 },
			new InputEventJoypadButton { ButtonIndex = JoyButton.DpadLeft });
		Register(
			TurnRight,
			new InputEventKey { Keycode = Key.Right },
			new InputEventKey { Keycode = Key.Kp6 },
			new InputEventJoypadButton { ButtonIndex = JoyButton.DpadRight });

		Register(ListSelectFirst, new InputEventKey { Keycode = Key.Home });
		Register(ListSelectLast, new InputEventKey { Keycode = Key.End });

		// Moved off Page Up/Down 2026-08-05 to free those keys for the
		// party cursor below, matching the classic Gold Box games exactly
		// (per the user's own reference screenshots) — mouse-wheel zoom
		// on the regional map already covers the same function.
		Register(
			RegionalMapZoomIn,
			new InputEventKey { Keycode = Key.Equal });
		Register(
			RegionalMapZoomOut,
			new InputEventKey { Keycode = Key.Minus });

		Register(
			PartyCursorPrevious,
			new InputEventKey { Keycode = Key.Pageup });
		Register(
			PartyCursorNext,
			new InputEventKey { Keycode = Key.Pagedown });

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

		// Ctrl+Alt+<letter>, not F5-F9 (moved off them 2026-07-28) — Godot's
		// own editor reserves F5 (Run), F6 (Run Scene), F7 (Pause), F8
		// (Stop), and F9 (Toggle Breakpoint), so those keys could get
		// intercepted by the editor instead of ever reaching the running
		// game, depending on focus/embedding. Matches the existing
		// DevToggleHighContrastTheme/DevToggleReducedMotion pattern below.
		Register(
			DevNextMockScenario,
			new InputEventKey
			{
				Keycode = Key.N,
				CtrlPressed = true,
				AltPressed = true,
			});
		Register(
			DevPreviousMockScenario,
			new InputEventKey
			{
				Keycode = Key.P,
				CtrlPressed = true,
				AltPressed = true,
			});
		Register(
			DevCycleResolutionPreset,
			new InputEventKey
			{
				Keycode = Key.R,
				CtrlPressed = true,
				AltPressed = true,
			});
		Register(
			DevAdvanceScriptedSession,
			new InputEventKey
			{
				Keycode = Key.S,
				CtrlPressed = true,
				AltPressed = true,
			});
		Register(
			DevCycleExplorationVariant,
			new InputEventKey
			{
				Keycode = Key.E,
				CtrlPressed = true,
				AltPressed = true,
			});
	}

	private static void Register(string action, params InputEvent[] events)
	{
		if (InputMap.HasAction(action))
		{
			return;
		}

		InputMap.AddAction(action);

		foreach (InputEvent inputEvent in events)
		{
			InputMap.ActionAddEvent(action, inputEvent);
		}
	}
}
