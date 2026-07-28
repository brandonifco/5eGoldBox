using Godot;

public partial class CommandBar : PanelContainer
{
	[Export]
	public bool Immersive { get; set; }

	private MarginContainer _commandMargin = null!;
	private HBoxContainer _commandLayout = null!;

	public override void _Ready()
	{
		_commandMargin = GetNode<MarginContainer>("%CommandMargin");
		_commandLayout = GetNode<HBoxContainer>("%CommandLayout");

		ApplyThemeVariations();
	}

	public void Clear()
	{
		foreach (Node child in _commandLayout.GetChildren())
		{
			_commandLayout.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void AddContent(Control content)
	{
		_commandLayout.AddChild(content);
	}

	private void ApplyThemeVariations()
	{
		if (!Immersive)
		{
			return;
		}

		ThemeTypeVariation = "ImmersiveOverlayPanel";
		_commandMargin.ThemeTypeVariation = "ImmersiveCommandMargin";
	}
}
