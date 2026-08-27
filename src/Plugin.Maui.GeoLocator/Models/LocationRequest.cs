namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Options for a one-shot location request.
/// </summary>
public sealed class LocationRequest
{
	/// <summary>
	/// Desired accuracy. Defaults to <see cref="LocationAccuracy.Medium"/>.
	/// </summary>
	public LocationAccuracy Accuracy { get; set; } = LocationAccuracy.Medium;

	/// <summary>
	/// How long to wait for a fresh location before failing. Defaults to 30 seconds.
	/// </summary>
	public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
