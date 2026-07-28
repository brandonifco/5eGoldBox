using Godot;

public partial class ComponentGallery : Control
{
	[Export]
	public Theme StandardTheme { get; set; } = null!;

	[Export]
	public Theme HighContrastTheme { get; set; } = null!;

	[Export]
	public PackedScene HotkeyCommandButtonScene { get; set; } = null!;

	[Export]
	public PackedScene ModalCardScene { get; set; } = null!;

	private HeaderBar _standardHeader = null!;
	private HeaderBar _immersiveHeader = null!;
	private PartySidebar _standardParty = null!;
	private PartySidebar _immersiveParty = null!;
	private MessageLog _standardMessage = null!;
	private MessageLog _immersiveMessage = null!;
	private CommandBar _standardCommands = null!;
	private CommandBar _immersiveCommands = null!;
	private SelectionList _selectionList = null!;
	private TooltipPanel _interactiveTooltip = null!;
	private ModalBackdrop _modalBackdrop = null!;
	private ConfirmationDialog _confirmationDialog = null!;
	private Button _tooltipTrigger = null!;
	private Button _themeToggleButton = null!;
	private Button _openModalButton = null!;
	private Button _standardConfirmationButton = null!;
	private Button _dangerousConfirmationButton = null!;
	private Button _disabledConfirmationButton = null!;
	private Label _statusLabel = null!;
	private HotkeyCommandButton _focusedCommandButton = null!;
	private bool _highContrast;

	public override void _Ready()
	{
		BindNodes();
		ConfigureChromeExamples();
		ConfigureSelectionExample();
		ConfigureTooltipExample();
		ConfigureModalExamples();
		_themeToggleButton.Pressed += ToggleTheme;
		_focusedCommandButton.CallDeferred(Control.MethodName.GrabFocus);
		UpdateStatus(
			"Gallery ready — inspect states, scroll sections, and open modal examples.");
	}

	public override void _UnhandledKeyInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventKey keyEvent ||
			!keyEvent.Pressed ||
			keyEvent.Echo)
		{
			return;
		}

		if (keyEvent.CtrlPressed &&
			keyEvent.AltPressed &&
			keyEvent.Keycode == Key.H)
		{
			ToggleTheme();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BindNodes()
	{
		_standardHeader = GetNode<HeaderBar>("%StandardHeader");
		_immersiveHeader = GetNode<HeaderBar>("%ImmersiveHeader");
		_standardParty = GetNode<PartySidebar>("%StandardParty");
		_immersiveParty = GetNode<PartySidebar>("%ImmersiveParty");
		_standardMessage = GetNode<MessageLog>("%StandardMessage");
		_immersiveMessage = GetNode<MessageLog>("%ImmersiveMessage");
		_standardCommands = GetNode<CommandBar>("%StandardCommands");
		_immersiveCommands = GetNode<CommandBar>("%ImmersiveCommands");
		_selectionList = GetNode<SelectionList>("%GallerySelectionList");
		_interactiveTooltip = GetNode<TooltipPanel>("%InteractiveTooltip");
		_modalBackdrop = GetNode<ModalBackdrop>("%GalleryModalBackdrop");
		_confirmationDialog =
			GetNode<ConfirmationDialog>("%GalleryConfirmationDialog");
		_tooltipTrigger = GetNode<Button>("%TooltipTrigger");
		_themeToggleButton = GetNode<Button>("%ThemeToggleButton");
		_openModalButton = GetNode<Button>("%OpenModalButton");
		_standardConfirmationButton =
			GetNode<Button>("%StandardConfirmationButton");
		_dangerousConfirmationButton =
			GetNode<Button>("%DangerousConfirmationButton");
		_disabledConfirmationButton =
			GetNode<Button>("%DisabledConfirmationButton");
		_statusLabel = GetNode<Label>("%StatusLabel");
	}

	private void ToggleTheme()
	{
		_highContrast = !_highContrast;
		Theme = _highContrast
			? HighContrastTheme
			: StandardTheme;
		UpdateStatus(
			_highContrast
				? "High-contrast gallery theme enabled."
				: "Standard gallery theme enabled.");
	}

	private void UpdateStatus(string message)
	{
		_statusLabel.Text = message;
	}
}
