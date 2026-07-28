using Godot;

public partial class StandardLayout : MarginContainer
{
	public AspectRatioContainer PresentationAspect { get; private set; } = null!;
	public PanelContainer PresentationSurface { get; private set; } = null!;
	public Control ExplorationView { get; private set; } = null!;
	public Control RegionalMapView { get; private set; } = null!;
	public Control CombatView { get; private set; } = null!;
	public HeaderBar HeaderBar { get; private set; } = null!;
	public PartySidebar PartySidebar { get; private set; } = null!;
	public MessageLog MessageLog { get; private set; } = null!;
	public CommandBar CommandBar { get; private set; } = null!;

	public override void _Ready()
	{
		PresentationAspect =
			GetNode<AspectRatioContainer>("%PresentationAspect");
		PresentationSurface =
			GetNode<PanelContainer>("%PresentationSurface");
		ExplorationView = GetNode<Control>("%ExplorationView");
		RegionalMapView = GetNode<Control>("%RegionalMapView");
		CombatView = GetNode<Control>("%CombatView");
		HeaderBar = GetNode<HeaderBar>("%HeaderBar");
		PartySidebar = GetNode<PartySidebar>("%PartySidebar");
		MessageLog = GetNode<MessageLog>("%MessageLog");
		CommandBar = GetNode<CommandBar>("%CommandBar");
	}
}
