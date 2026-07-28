using System.Collections.Generic;
using System.Linq;

// One method per §10.2 "Regional map" minimum case.
internal static class MockRegionalMapScenarios
{
	public static RegionalMapViewModel NoNearbyLocations()
	{
		return new RegionalMapViewModel(
			"frontier-region",
			PartyMarker(),
			System.Array.Empty<RegionalMapMarkerViewModel>());
	}

	public static RegionalMapViewModel DenseLocationCluster()
	{
		return new RegionalMapViewModel(
			"frontier-region",
			PartyMarker(),
			Enumerable.Range(0, 8)
				.Select(index => new RegionalMapMarkerViewModel(
					$"location-{index}",
					$"Site {index + 1}",
					new RegionalMapPointViewModel(index * 10, (index % 3) * 15)))
				.ToList());
	}

	public static RegionalMapViewModel SelectedLocation()
	{
		return new RegionalMapViewModel(
			"frontier-region",
			PartyMarker(),
			new[]
			{
				new RegionalMapMarkerViewModel(
					"ruined-watchtower",
					"Ruined Watchtower",
					new RegionalMapPointViewModel(40, 20),
					Selected: true),
			},
			SelectedLocationId: "ruined-watchtower");
	}

	public static RegionalMapViewModel RoutePreview()
	{
		return new RegionalMapViewModel(
			"frontier-region",
			PartyMarker(),
			new[]
			{
				new RegionalMapMarkerViewModel(
					"ruined-watchtower",
					"Ruined Watchtower",
					new RegionalMapPointViewModel(40, 20)),
			},
			RoutePreview: new[]
			{
				new RegionalMapPointViewModel(0, 0),
				new RegionalMapPointViewModel(20, 10),
				new RegionalMapPointViewModel(40, 20),
			});
	}

	public static RegionalMapViewModel TravelWarning()
	{
		return new RegionalMapViewModel(
			"frontier-region",
			PartyMarker(),
			new[]
			{
				new RegionalMapMarkerViewModel(
					"haunted-marsh",
					"Haunted Marsh (dangerous)",
					new RegionalMapPointViewModel(15, 35)),
			});
	}

	private static RegionalMapMarkerViewModel PartyMarker()
	{
		return new RegionalMapMarkerViewModel(
			"party",
			"Party",
			new RegionalMapPointViewModel(0, 0));
	}
}
