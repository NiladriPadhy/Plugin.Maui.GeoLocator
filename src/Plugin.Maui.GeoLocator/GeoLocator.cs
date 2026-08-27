namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Entry point for the GeoLocator plugin when dependency injection is not used.
/// </summary>
public static class GeoLocator
{
	static IGeoLocator? _current;

	/// <summary>
	/// Gets the shared <see cref="IGeoLocator"/> instance.
	/// </summary>
	public static IGeoLocator Current => _current ??= new GeoLocatorImplementation();

	/// <summary>
	/// Replaces the shared instance. Intended for tests and custom implementations.
	/// </summary>
	public static void SetDefault(IGeoLocator implementation) =>
		_current = implementation ?? throw new ArgumentNullException(nameof(implementation));
}
