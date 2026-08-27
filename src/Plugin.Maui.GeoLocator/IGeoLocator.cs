using Microsoft.Maui.ApplicationModel;

namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Cross-platform geolocation, tracking, and reverse geocoding for .NET MAUI.
/// </summary>
public interface IGeoLocator
{
	/// <summary>
	/// Gets a value indicating whether location services are enabled on the device.
	/// </summary>
	bool IsEnabled { get; }

	/// <summary>
	/// Gets a value indicating whether continuous location tracking is active.
	/// </summary>
	bool IsTracking { get; }

	/// <summary>
	/// Gets a value indicating whether plugin logging is currently enabled.
	/// </summary>
	bool IsLoggingEnabled { get; }

	/// <summary>
	/// Raised when a new location is received during tracking.
	/// </summary>
	event EventHandler<LocationChangedEventArgs>? LocationChanged;

	/// <summary>
	/// Raised when tracking or a location request fails after it has started.
	/// </summary>
	event EventHandler<LocationErrorEventArgs>? LocationError;

	/// <summary>
	/// Enables or disables plugin logging.
	/// </summary>
	/// <param name="enabled">Whether logging should be enabled.</param>
	/// <param name="logger">Optional logger. When omitted, a debug logger is used.</param>
	void EnableLogging(bool enabled, IGeoLocatorLogger? logger = null);

	/// <summary>
	/// Checks the current when-in-use location permission without prompting the user.
	/// </summary>
	Task<PermissionStatus> CheckPermissionAsync();

	/// <summary>
	/// Requests when-in-use location permission from the user if it has not already been granted.
	/// </summary>
	Task<PermissionStatus> RequestPermissionAsync();

	/// <summary>
	/// Returns the last cached location, or <c>null</c> if none is available.
	/// </summary>
	Task<GeoPosition?> GetLastKnownLocationAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Queries the device for the current location.
	/// </summary>
	Task<GeoPosition?> GetCurrentLocationAsync(LocationRequest? request = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts continuous location tracking. Raises <see cref="LocationChanged"/> for each update.
	/// </summary>
	Task StartTrackingAsync(TrackingOptions? options = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops continuous location tracking.
	/// </summary>
	Task StopTrackingAsync();

	/// <summary>
	/// Converts coordinates into human-readable addresses.
	/// </summary>
	Task<IReadOnlyList<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude, int maxResults = 5, CancellationToken cancellationToken = default);
}
