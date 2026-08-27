namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Options for a continuous location tracking session.
/// </summary>
public sealed class TrackingOptions
{
	/// <summary>
	/// Desired accuracy. Defaults to <see cref="LocationAccuracy.Medium"/>.
	/// </summary>
	public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.Medium;

	/// <summary>
	/// Minimum time between updates. Honored on Android. Defaults to 1 second.
	/// </summary>
	public TimeSpan MinimumTime { get; set; } = TimeSpan.FromSeconds(1);

	/// <summary>
	/// Minimum distance, in meters, that the device must move before another update is delivered.
	/// </summary>
	public double MinimumDistanceMeters { get; set; } = 10;

	/// <summary>
	/// When <c>true</c>, also request heading updates on platforms that support them.
	/// </summary>
	public bool IncludeHeading { get; set; }

	/// <summary>
	/// When <c>true</c>, requests background updates on iOS. The host app must declare the
	/// <c>location</c> background mode and always-authorization usage strings.
	/// Android background tracking requires a foreground service in the host app.
	/// </summary>
	public bool AllowBackgroundUpdates { get; set; }
}
