using Godot;

public partial class ExplorationView : Control
{
	private ColorRect _placeholderBackground = null!;
	private TextureRect _explorationImage = null!;
	private Label _label = null!;

	public override void _Ready()
	{
		_placeholderBackground = GetNode<ColorRect>("%PlaceholderBackground");
		_explorationImage = GetNode<TextureRect>("%ExplorationImage");
		_label = GetNode<Label>("%ExplorationLabel");
	}

	// M6b: plain colored placeholders for scene keys with no real art yet
	// ("placeholders are just fine, function before beauty" — explicit
	// creator direction). The one location with real art (the outpost
	// entrance) keeps showing its actual image instead. Facing/compass/
	// overlay-prompt display (M6f) is still deliberately not wired here —
	// that needs its own creator approval per the governing plan.
	internal void Configure(ExplorationViewModel model)
	{
		if (model.SceneKey == ExplorationSceneKeys.OutpostEntrance)
		{
			_placeholderBackground.Hide();
			_explorationImage.Show();
			_label.Text = "Outpost";
			return;
		}

		_explorationImage.Hide();
		_placeholderBackground.Show();
		_placeholderBackground.Color = ResolvePlaceholderColor(model.SceneKey);
		_label.Text = ResolveDisplayLabel(model.SceneKey);
	}

	private static Color ResolvePlaceholderColor(string sceneKey)
	{
		return sceneKey switch
		{
			ExplorationSceneKeys.BuildingInterior => new Color(0.36f, 0.24f, 0.16f),
			ExplorationSceneKeys.TownStreet => new Color(0.42f, 0.50f, 0.58f),
			ExplorationSceneKeys.Cavern => new Color(0.16f, 0.15f, 0.14f),
			ExplorationSceneKeys.DungeonCorridor => new Color(0.22f, 0.22f, 0.26f),
			_ => new Color(0.3f, 0.3f, 0.3f),
		};
	}

	private static string ResolveDisplayLabel(string sceneKey)
	{
		return sceneKey switch
		{
			ExplorationSceneKeys.BuildingInterior => "Building Interior",
			ExplorationSceneKeys.TownStreet => "Town Street",
			ExplorationSceneKeys.Cavern => "Cavern",
			ExplorationSceneKeys.DungeonCorridor => "Dungeon Corridor",
			_ => sceneKey,
		};
	}
}
