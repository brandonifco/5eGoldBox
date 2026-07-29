using System.Linq;
using Godot;

// Marker pins and the route-preview line — split out of RegionalMapView.cs
// (see that file's header) once the combined class crossed the governing
// plan's 250-line review threshold.
public partial class RegionalMapView
{
	private void RebuildMarkers()
	{
		foreach (RegionalMapMarkerPin pin in _pins)
		{
			_markersLayer.RemoveChild(pin);
			pin.QueueFree();
		}

		_pins.Clear();

		foreach (RegionalMapMarkerViewModel marker in _locationMarkers)
		{
			AddPin(marker.Position, RegionalMapMarkerPin.PinKind.Location, marker.Selected);
		}

		if (_partyMarker is not null)
		{
			AddPin(_partyMarker.Position, RegionalMapMarkerPin.PinKind.Party, selected: false);
		}
	}

	private void AddPin(
		RegionalMapPointViewModel position,
		RegionalMapMarkerPin.PinKind kind,
		bool selected)
	{
		RegionalMapMarkerPin pin = new();
		_markersLayer.AddChild(pin);
		pin.Configure(kind, selected);
		PositionPin(pin, position);
		_pins.Add(pin);
	}

	private void RepositionMarkers()
	{
		int index = 0;

		foreach (RegionalMapMarkerViewModel marker in _locationMarkers)
		{
			PositionPin(_pins[index], marker.Position);
			index++;
		}

		if (_partyMarker is not null)
		{
			PositionPin(_pins[index], _partyMarker.Position);
		}
	}

	private void PositionPin(RegionalMapMarkerPin pin, RegionalMapPointViewModel position)
	{
		Vector2 mapSize = _mapImage.Size;
		Vector2 center = new(
			(float)position.X * mapSize.X,
			(float)position.Y * mapSize.Y);
		float radius = pin.Diameter / 2f;

		pin.Position = center - new Vector2(radius, radius);
		pin.Size = new Vector2(pin.Diameter, pin.Diameter);
	}

	// M7c: draws exactly the supplied points, nothing calculated —
	// "visual route previews without calculating authoritative paths."
	private void DrawRoutePreview()
	{
		if (_routePreview is not { Count: >= 2 })
		{
			return;
		}

		Vector2 mapSize = _mapImage.Size;
		Vector2[] points = _routePreview
			.Select(point => new Vector2(
				(float)point.X * mapSize.X,
				(float)point.Y * mapSize.Y))
			.ToArray();

		_routeOverlay.DrawPolyline(
			points,
			new Color(0.9254902f, 0.78039217f, 0.36078432f, 0.8f),
			3f,
			antialiased: true);
	}
}
