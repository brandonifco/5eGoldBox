using System;
using FiveEGoldBox.Application.Exploration;
using FiveEGoldBox.Application.Scenarios;
using FiveEGoldBox.Application.Sessions;
using FiveEGoldBox.Core.Runtime;
using Godot;

public partial class AppShell : Control
{
	// Matches FiveEGoldBox.Console's own default (Program.cs) — the same
	// scenario, same convention: a client names a scenario ID string and
	// knows nothing else about it.
	private const string DefaultScenarioId = "scenario.watchtower";
	private const int DefaultRandomSeed = 20260728;

	// A developer-only override, same trusted-absolutely/fail-loudly
	// convention as FIVEEGOLDBOX_DATA_ROOT (DataDirectoryLocator, in
	// Application/Content/): when FIVEEGOLDBOX_DEBUG_LOCATION_ID is set, the
	// app boots straight into that exploration location/floor/position/
	// facing instead of the scenario's outpost. A partially-set debug env
	// var group is treated as a misconfiguration and fails loudly rather
	// than silently falling back to the normal outpost start — a typo'd var
	// name should never look like "nothing happened".
	private const string DebugLocationIdEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_LOCATION_ID";
	// Optional, unlike the other five below -- every existing documented
	// debug example (CLAUDE.md) predates this var and targets Watchtower
	// without setting it, so it defaults to DefaultScenarioId rather than
	// joining the "set LOCATION_ID, these become required" group. Only
	// needed to jump into a location that isn't scenario.watchtower's own
	// (e.g. Hollow Mill's), the same scenario-plus-location pair
	// FiveEGoldBox.Console's own `goto` verb already takes explicitly.
	private const string DebugScenarioIdEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_SCENARIO_ID";
	private const string DebugFloorEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_FLOOR";
	private const string DebugXEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_X";
	private const string DebugYEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_Y";
	private const string DebugFacingEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_FACING";
	private const string DebugProgressIdEnvironmentVariable =
		"FIVEEGOLDBOX_DEBUG_PROGRESS_ID";

	private StandardLayout _standardLayout = null!;
	private ImmersiveLayout _immersiveLayout = null!;
	private ConfirmationDialog _confirmationDialog = null!;
	private ModalScreenView _modalScreenView = null!;

	private ShellCommandBarController _commandBarController = null!;
	private ShellConfirmationController _confirmationController = null!;
	private ShellModalScreenController _modalScreenController = null!;
	private ShellInputRouter _inputRouter = null!;
	private ShellInteractionController _interactionController = null!;
	private ShellLayoutController _layoutController = null!;
	private ShellPartyPreviewController _partyPreviewController = null!;
	private ShellPresentationController _presentationController = null!;
	private ShellThemeController _themeController = null!;
	private MockScenarioPicker _scenarioPicker = null!;
	private MockScriptedSession _scriptedSession = null!;
	private int _scriptedSessionStepIndex;
	private RealGameSession _realSession = null!;

	public override void _Ready()
	{
		PlayerInputActions.EnsureRegistered();

		// project.godot's window/size/mode=2 sets the same thing, but isn't
		// reliably honored by every window manager at initial map time --
		// confirmed live, the window opened un-maximized despite that
		// setting. Setting it again here, once the window actually exists,
		// is the reliable fallback.
		GetWindow().Mode = Window.ModeEnum.Maximized;

		_standardLayout = GetNode<StandardLayout>("%StandardLayout");
		_immersiveLayout = GetNode<ImmersiveLayout>("%ImmersiveLayout");
		_confirmationDialog = GetNode<ConfirmationDialog>("%ConfirmationDialog");
		_modalScreenView = GetNode<ModalScreenView>("%ModalScreenView");

		_presentationController = new ShellPresentationController(
			_standardLayout.ExplorationView,
			_standardLayout.RegionalMapView,
			_standardLayout.CombatView,
			_standardLayout.AreaMapView,
			_standardLayout.HeaderBar,
			_immersiveLayout.HeaderBar,
			_standardLayout.MessageLog,
			_immersiveLayout.MessageLog);

		_themeController = new ShellThemeController(
			this,
			Theme,
			GD.Load<Theme>(
				"res://ui/themes/GameUiHighContrastTheme.tres"));

		_partyPreviewController = new ShellPartyPreviewController(
			_standardLayout.PartySidebar,
			_immersiveLayout.PartySidebar);

		_layoutController = new ShellLayoutController(
			GetWindow(),
			_standardLayout,
			_immersiveLayout,
			_standardLayout.PresentationAspect,
			_immersiveLayout.PresentationAspect,
			_standardLayout.PresentationSurface,
			RefreshImmersivePartyPreview,
			RefreshCommandBars);

		_commandBarController = new ShellCommandBarController(
			_standardLayout.CommandBar,
			_immersiveLayout.CommandBar,
			_layoutController,
			GD.Load<PackedScene>(
				"res://ui/components/commands/" +
					"HotkeyCommandButton.tscn"));

		_confirmationController =
			new ShellConfirmationController(_confirmationDialog);
		_modalScreenController =
			new ShellModalScreenController(_modalScreenView);

		_interactionController = new ShellInteractionController(
			_presentationController,
			_commandBarController,
			_confirmationController,
			_modalScreenController,
			_partyPreviewController,
			() => _themeController.Settings.ThemeVariant ==
				UiThemeVariant.HighContrast,
			() => _themeController.Settings.MotionPreference ==
				UiMotionPreference.Reduced);

		_scenarioPicker = new MockScenarioPicker();
		_scriptedSession = new MockScriptedSession();

		_inputRouter = new ShellInputRouter(
			_interactionController,
			ToggleImmersiveMode,
			ShowExplorationView,
			ShowRegionalMapView,
			ShowCombatView,
			ToggleHighContrastTheme,
			ToggleReducedMotion,
			ExitExplorationMovementMode,
			ReportMovement,
			ShowNextMockScenario,
			ShowPreviousMockScenario,
			CycleResolutionPreset,
			AdvanceScriptedSession,
			CycleExplorationVariant,
			_presentationController.CycleRegionalMapZoom,
			_interactionController.CancelCombatTargeting,
			_interactionController.CyclePartyHighlight);

		_themeController.Initialize();
		_layoutController.Initialize();

		// The real integration seam (RealGameSession) is now the default
		// experience rather than a dev-only path — mock content
		// (MockScenarioPicker, MockScriptedSession, the F-key dev
		// shortcuts) still exists unchanged for QA, but starting the app
		// now plays an actual scenario through the actual engine,
		// including tactical combat (RealCombatSession, wired through
		// ShellInteractionController.RealCombat.cs) for Move, single-
		// target Weapon Attack, and End Turn — spellcasting and multi-
		// target attacks are proven at the backend but not wired here yet.
		_realSession = CreateRealSession();
		_interactionController.ShowRealSession(_realSession);
	}

	// Builds the session AppShell hands to RealGameSession. The normal path
	// is unchanged from before this override existed; the debug path only
	// engages when FIVEEGOLDBOX_DEBUG_LOCATION_ID is actually set.
	private static RealGameSession CreateRealSession()
	{
		string? debugLocationId = System.Environment.GetEnvironmentVariable(
			DebugLocationIdEnvironmentVariable);

		if (string.IsNullOrEmpty(debugLocationId))
		{
			return new RealGameSession(DefaultScenarioId, DefaultRandomSeed);
		}

		string floor = RequireDebugEnvironmentVariable(
			DebugFloorEnvironmentVariable);
		int x = RequireDebugIntEnvironmentVariable(DebugXEnvironmentVariable);
		int y = RequireDebugIntEnvironmentVariable(DebugYEnvironmentVariable);
		ExplorationFacing facing = RequireDebugFacingEnvironmentVariable(
			DebugFacingEnvironmentVariable);
		string? progressId = System.Environment.GetEnvironmentVariable(
			DebugProgressIdEnvironmentVariable);
		string? debugScenarioId = System.Environment.GetEnvironmentVariable(
			DebugScenarioIdEnvironmentVariable);
		string scenarioId = string.IsNullOrEmpty(debugScenarioId)
			? DefaultScenarioId
			: debugScenarioId;

		ApplicationSessionState state =
			ScenarioSessionFactory.CreateAtExploration(
				scenarioId,
				debugLocationId,
				floor,
				new GridPosition(x, y),
				facing,
				DefaultRandomSeed,
				string.IsNullOrEmpty(progressId) ? null : progressId);

		return new RealGameSession(state);
	}

	private static string RequireDebugEnvironmentVariable(
		string variableName)
	{
		string? value = System.Environment.GetEnvironmentVariable(variableName);

		if (string.IsNullOrEmpty(value))
		{
			throw new InvalidOperationException(
				$"{DebugLocationIdEnvironmentVariable} is set, which requires " +
					$"{variableName} to also be set, but it is missing or empty.");
		}

		return value;
	}

	private static int RequireDebugIntEnvironmentVariable(
		string variableName)
	{
		string value = RequireDebugEnvironmentVariable(variableName);

		if (!int.TryParse(value, out int parsed))
		{
			throw new InvalidOperationException(
				$"{variableName} is set to '{value}', which is not a valid integer.");
		}

		return parsed;
	}

	private static ExplorationFacing RequireDebugFacingEnvironmentVariable(
		string variableName)
	{
		string value = RequireDebugEnvironmentVariable(variableName);

		if (!Enum.TryParse(
				value,
				ignoreCase: true,
				out ExplorationFacing facing)
			|| !Enum.IsDefined(facing))
		{
			throw new InvalidOperationException(
				$"{variableName} is set to '{value}', which is not a valid facing.");
		}

		return facing;
	}
}
