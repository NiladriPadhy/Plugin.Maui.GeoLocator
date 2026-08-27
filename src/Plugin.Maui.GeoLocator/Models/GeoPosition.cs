namespace Plugin.Maui.GeoLocator;

/// <summary>
/// A geographic position reported by the device.
/// </summary>
public sealed class GeoPosition
{
	public double Latitude { get; init; }

	public double Longitude { get; init; }

	public double? Altitude { get; init; }

	/// <summary>Estimated horizontal accuracy in meters.</summary>
	public double? Accuracy { get; init; }

	/// <summary>Estimated vertical accuracy in meters.</summary>
	public double? VerticalAccuracy { get; init; }

	/// <summary>Speed in meters per second, when available.</summary>
	public double? Speed { get; init; }

	/// <summary>Heading in degrees relative to true north, when available.</summary>
	public double? Heading { get; init; }

	public DateTimeOffset Timestamp { get; init; }

	public bool IsFromMockProvider { get; init; }

	/// <summary>
	/// <c>true</c> when the platform delivered a reduced-accuracy location (for example iOS Approximate Location).
	/// </summary>
	public bool ReducedAccuracy { get; init; }

	public override string ToString() =>
		$"{Latitude:F6}, {Longitude:F6} (±{Accuracy?.ToString("0") ?? "?"} m) @ {Timestamp:u}";

	/// <summary>
	/// Calculates the great-circle distance between two positions using the haversine formula.
	/// </summary>
	public static double CalculateDistance(GeoPosition from, GeoPosition to, DistanceUnit unit = DistanceUnit.Kilometers)
	{
		ArgumentNullException.ThrowIfNull(from);
		ArgumentNullException.ThrowIfNull(to);
		return CalculateDistance(from.Latitude, from.Longitude, to.Latitude, to.Longitude, unit);
	}

	/// <summary>
	/// Calculates the great-circle distance between two coordinate pairs using the haversine formula.
	/// </summary>
	public static double CalculateDistance(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude, DistanceUnit unit = DistanceUnit.Kilometers)
	{
		const double earthRadiusKm = 6371.0;
		var dLat = DegreesToRadians(toLatitude - fromLatitude);
		var dLon = DegreesToRadians(toLongitude - fromLongitude);
		var lat1 = DegreesToRadians(fromLatitude);
		var lat2 = DegreesToRadians(toLatitude);

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
		var kilometers = earthRadiusKm * c;

		return unit switch
		{
			DistanceUnit.Miles => kilometers * 0.621371192,
			DistanceUnit.Meters => kilometers * 1000,
			_ => kilometers
		};
	}

	static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
