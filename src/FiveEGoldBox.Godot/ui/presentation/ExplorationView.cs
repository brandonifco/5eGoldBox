using Godot;

public partial class ExplorationView : Control
{
	private Label _debugLabel = null!;

	public override void _Ready()
	{
		_debugLabel = GetNode<Label>("%ExplorationLabel");
	}

	// Per-scene-key art (M6b) and facing/compass/overlay display (M6f,
	// creator-approved) aren't wired here on purpose — this step is just
	// making the view a reusable, data-driven scene. The image stays the
	// single existing placeholder texture regardless of model content.
	internal void Configure(ExplorationViewModel model)
	{
		_debugLabel.Text = model.SceneKey;
	}
}
