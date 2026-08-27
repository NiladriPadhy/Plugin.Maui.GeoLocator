namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Categorized failure reasons for location and geocoding operations.
/// </summary>
public enum GeoLocatorError
{
	Unknown,
	PermissionDenied,
	LocationDisabled,
	Timeout,
	Unavailable,
	FeatureNotSupported
}
