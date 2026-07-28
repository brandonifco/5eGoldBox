using Godot;

public partial class ImmersiveLayout : Control
{
	public AspectRatioContainer PresentationAspect { get; private set; } = null!;
	public HeaderBar HeaderBar { get; private set; } = null!;
	public PartySidebar PartySidebar { get; private set; } = null!;
	public MessageLog MessageLog { get; private set; } = null!;
	public CommandBar CommandBar { get; private set; } = null!;

	public override void _Ready()
	{
		PresentationAspect =
			GetNode<AspectRatioContainer>("%PresentationAspect");
		HeaderBar = GetNode<HeaderBar>("%HeaderBar");
		PartySidebar = GetNode<PartySidebar>("%PartySidebar");
		MessageLog = GetNode<MessageLog>("%MessageLog");
		CommandBar = GetNode<CommandBar>("%CommandBar");
	}
}
