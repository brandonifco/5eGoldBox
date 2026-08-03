using System.Collections.Generic;
using Godot;

public partial class ExplorationView : Control
{
	private ColorRect _placeholderBackground = null!;
	private TextureRect _explorationImage = null!;
	private Label _label = null!;
	private Control _facingBadge = null!;
	private Label _facingLabel = null!;
	private Control _compassBadge = null!;
	private Label _compassLabel = null!;
	private Control _overlayPromptPanel = null!;
	private Label _overlayPromptLabel = null!;
	private DungeonCorridor3DView _corridorView = null!;
	private Label _coordinateLabel = null!;

	public override void _Ready()
	{
		_placeholderBackground = GetNode<ColorRect>("%PlaceholderBackground");
		_explorationImage = GetNode<TextureRect>("%ExplorationImage");
		_label = GetNode<Label>("%ExplorationLabel");
		_facingBadge = GetNode<Control>("%FacingBadge");
		_facingLabel = GetNode<Label>("%FacingLabel");
		_compassBadge = GetNode<Control>("%CompassBadge");
		_compassLabel = GetNode<Label>("%CompassLabel");
		_overlayPromptPanel = GetNode<Control>("%OverlayPromptPanel");
		_overlayPromptLabel = GetNode<Label>("%OverlayPromptLabel");

		// Script-only, no .tscn entry -- same convention
		// PartyDirectionMarker/CombatHighlightCell already use, extended
		// by DungeonCorridor3DView itself to build its own embedded
		// SubViewport/Camera3D/geometry at runtime. Inserted right after
		// the flat placeholder/outpost image, so real corridor art
		// layers over either of them but still sits below the
		// label/facing/compass/overlay-prompt nodes.
		_corridorView = new DungeonCorridor3DView();
		AddChild(_corridorView);
		_corridorView.SetAnchorsPreset(LayoutPreset.FullRect);
		MoveChild(_corridorView, _explorationImage.GetIndex() + 1);

		// A debug aid, not production polish -- the real X/Y this view is
		// actually rendering, requested while diagnosing the corridor
		// renderer against real map data by hand. Bottom-left corner,
		// independent of the existing facing/compass badge row.
		_coordinateLabel = new Label
		{
			Visible = false,
			MouseFilter = MouseFilterEnum.Ignore,
			Modulate = new Color(1f, 1f, 1f, 0.8f),
		};
		_coordinateLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.9f));
		_coordinateLabel.AddThemeConstantOverride("shadow_offset_x", 1);
		_coordinateLabel.AddThemeConstantOverride("shadow_offset_y", 1);
		AddChild(_coordinateLabel);
		_coordinateLabel.SetAnchorsPreset(LayoutPreset.BottomLeft);
		_coordinateLabel.Position = new Vector2(8, -24);
	}

	// M6b: plain colored placeholders for scene keys with no real art yet
	// ("placeholders are just fine, function before beauty" — explicit
	// creator direction). The one location with real art (the outpost
	// entrance) keeps showing its actual image instead.
	internal void Configure(ExplorationViewModel model)
	{
		ConfigureScene(model.SceneKey);
		ConfigureFacing(model.FacingIndicatorText, model.CompassText);
		ConfigureOverlayPrompts(model.OverlayPrompts);
	}

	private void ConfigureScene(string sceneKey)
	{
		if (sceneKey == ExplorationSceneKeys.OutpostEntrance)
		{
			_placeholderBackground.Hide();
			_explorationImage.Show();
			_label.Text = "Outpost";
			return;
		}

		_explorationImage.Hide();
		_placeholderBackground.Show();
		_placeholderBackground.Color = ResolvePlaceholderColor(sceneKey);
		_label.Text = ResolveDisplayLabel(sceneKey);
	}

	// M6f: facing/compass badges and the overlay-prompt panel — the three
	// ExplorationViewModel fields M6a declared but left unwired pending
	// creator approval, now that it's given. All three are null/empty by
	// default, so a caller that never sets them (dev tools cycling scene
	// keys only) just keeps them hidden, matching prior behavior.
	private void ConfigureFacing(string? facingIndicatorText, string? compassText)
	{
		_facingBadge.Visible = !string.IsNullOrEmpty(facingIndicatorText);
		_facingLabel.Text = facingIndicatorText ?? string.Empty;

		_compassBadge.Visible = !string.IsNullOrEmpty(compassText);
		_compassLabel.Text = compassText ?? string.Empty;
	}

	// Layers real, tile-based first-person wall art on top of whatever
	// ConfigureScene already put down (flat color for a real session's
	// still-placeholder SceneKey, or the outpost's own real image) --
	// null clears it back to that plain background, which is what every
	// non-exploration mode and every mock-content call site wants. See
	// docs/2026-08-03-sbs-dungeon-tileset-inventory.md for why this
	// renders from the same AreaMapViewModel AreaMapView already
	// consumes rather than a new Application-side projection.
	internal void ConfigureCorridor(AreaMapViewModel? map)
	{
		if (map is null)
		{
			_corridorView.Clear();
			_coordinateLabel.Visible = false;
			return;
		}

		CorridorGeometry geometry = ResolveCorridor(
			map,
			radius: DungeonCorridor3DView.ConfigureRadius);

		_corridorView.Configure(geometry, DungeonWallMaterials.ResolveDefault());

		_coordinateLabel.Visible = true;
		_coordinateLabel.Text = $"({map.PartyX}, {map.PartyY}) {map.PartyFacing}";
	}

	private void ConfigureOverlayPrompts(IReadOnlyList<string>? overlayPrompts)
	{
		bool hasPrompts = overlayPrompts is { Count: > 0 };

		_overlayPromptPanel.Visible = hasPrompts;
		_overlayPromptLabel.Text = hasPrompts
			? string.Join("\n", overlayPrompts!)
			: string.Empty;
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
