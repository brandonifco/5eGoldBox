using Godot;

public partial class AppShell : Control
{
	// Matches FiveEGoldBox.Console's own default (Program.cs) — the same
	// scenario, same convention: a client names a scenario ID string and
	// knows nothing else about it.
	private const string DefaultScenarioId = "scenario.watchtower";
	private const int DefaultRandomSeed = 20260728;

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

		_standardLayout = GetNode<StandardLayout>("%StandardLayout");
		_immersiveLayout = GetNode<ImmersiveLayout>("%ImmersiveLayout");
		_confirmationDialog = GetNode<ConfirmationDialog>("%ConfirmationDialog");
		_modalScreenView = GetNode<ModalScreenView>("%ModalScreenView");

		_presentationController = new ShellPresentationController(
			_standardLayout.ExplorationView,
			_standardLayout.RegionalMapView,
			_standardLayout.CombatView,
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
			_interactionController.CancelCombatTargeting);

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
		_realSession = new RealGameSession(DefaultScenarioId, DefaultRandomSeed);
		_interactionController.ShowRealSession(_realSession);
	}
}
