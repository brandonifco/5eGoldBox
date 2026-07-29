using System.Collections.Generic;

// The Frontier Region map's real driving content — analogous to
// MockExplorationCommandContent's role for the exploration command bar
// (M6c): not the QA/stress-test catalog (that's MockRegionalMapScenarios,
// M5c, "makes no claim about backend rule behavior"), this is the actual
// pre-integration content the live RegionalMapView renders, calibrated
// against images/regional_map_01.png's own baked-in location art and
// labels. Positions are normalized (0.0-1.0) fractions of the map image's
// width/height, read by eye against the source art — flagged for a human
// look the same way M6b's placeholder colors were, since no build check
// can confirm pixel-perfect alignment against the art.
internal static class MockRegionalMapContent
{
	public static readonly RegionalLocationDefinition Outpost = new(
		RegionalLocationIds.Outpost,
		"Frontier Outpost",
		new RegionalMapPointViewModel(0.374, 0.427),
		ExplorationSceneKeys.OutpostEntrance,
		"A fortified waypost where the party's journey begins.");

	public static readonly IReadOnlyList<RegionalLocationDefinition> Locations =
		new[]
		{
			Outpost,
			new RegionalLocationDefinition(
				RegionalLocationIds.Northhold,
				"Northhold",
				new RegionalMapPointViewModel(0.472, 0.145),
				ExplorationSceneKeys.TownStreet,
				"A walled town at the foot of the northern mountains."),
			new RegionalLocationDefinition(
				RegionalLocationIds.OldMine,
				"Old Mine",
				new RegionalMapPointViewModel(0.135, 0.330),
				ExplorationSceneKeys.Cavern,
				"An abandoned mine cut into the mountainside."),
			new RegionalLocationDefinition(
				RegionalLocationIds.Mirefen,
				"Mirefen",
				new RegionalMapPointViewModel(0.845, 0.175),
				ExplorationSceneKeys.BuildingInterior,
				"A small hamlet on the edge of a misty marsh."),
			new RegionalLocationDefinition(
				RegionalLocationIds.Lakeside,
				"Lakeside",
				new RegionalMapPointViewModel(0.108, 0.610),
				ExplorationSceneKeys.BuildingInterior,
				"A fishing village on the shore of a quiet lake."),
			new RegionalLocationDefinition(
				RegionalLocationIds.Seagate,
				"Seagate",
				new RegionalMapPointViewModel(0.690, 0.700),
				ExplorationSceneKeys.TownStreet,
				"A busy port town where the river meets the sea."),
		};

	public static RegionalLocationDefinition? Find(string locationId)
	{
		foreach (RegionalLocationDefinition location in Locations)
		{
			if (location.Id == locationId)
			{
				return location;
			}
		}

		return null;
	}

	public static RegionalMapViewModel BuildViewModel(string? selectedLocationId)
	{
		List<RegionalMapMarkerViewModel> markers = new();

		foreach (RegionalLocationDefinition location in Locations)
		{
			markers.Add(new RegionalMapMarkerViewModel(
				location.Id,
				location.Label,
				location.Position,
				Selected: location.Id == selectedLocationId,
				Description: location.Description));
		}

		return new RegionalMapViewModel(
			"frontier-region",
			new RegionalMapMarkerViewModel(
				"party",
				"Party",
				Outpost.Position),
			markers,
			selectedLocationId,
			BuildRoutePreview(selectedLocationId));
	}

	// M7c: a straight supplied preview between the party's current
	// position and whatever's selected — not an authoritative path,
	// there is no pathfinding here at all, just two points to draw a
	// line between.
	private static IReadOnlyList<RegionalMapPointViewModel>? BuildRoutePreview(
		string? selectedLocationId)
	{
		if (selectedLocationId is null ||
			selectedLocationId == RegionalLocationIds.Outpost)
		{
			return null;
		}

		RegionalLocationDefinition? destination = Find(selectedLocationId);

		if (destination is null)
		{
			return null;
		}

		return new[] { Outpost.Position, destination.Position };
	}
}
