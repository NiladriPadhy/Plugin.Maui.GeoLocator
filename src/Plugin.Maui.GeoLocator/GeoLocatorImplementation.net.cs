#if !ANDROID && !IOS

namespace Plugin.Maui.GeoLocator;

partial class GeoLocatorImplementation
{
	public bool IsEnabled => false;

	public Task<GeoPosition?> GetLastKnownLocationAsync(CancellationToken cancellationToken = default) =>
		Task.FromException<GeoPosition?>(FeatureNotSupported());

	public Task<GeoPosition?> GetCurrentLocationAsync(LocationRequest? request = null, CancellationToken cancellationToken = default) =>
		Task.FromException<GeoPosition?>(FeatureNotSupported());

	public Task StartTrackingAsync(TrackingOptions? options = null, CancellationToken cancellationToken = default) =>
		Task.FromException(FeatureNotSupported());

	public Task StopTrackingAsync() => Task.CompletedTask;

	public Task<IReadOnlyList<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude, int maxResults = 5, CancellationToken cancellationToken = default) =>
		Task.FromException<IReadOnlyList<GeoAddress>>(FeatureNotSupported());

	static GeoLocatorException FeatureNotSupported() =>
		new(GeoLocatorError.FeatureNotSupported, "GeoLocator is only supported on Android and iOS.");
}

#endif
