namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Shared configuration applied when the plugin is registered with <c>UseGeoLocator</c>.
/// </summary>
public sealed class GeoLocatorOptions
{
	/// <summary>
	/// Gets or sets a value indicating whether plugin logging starts enabled.
	/// </summary>
	public bool EnableLogging { get; set; }

	/// <summary>
	/// Gets or sets a custom logger. When <c>null</c>, the plugin uses Microsoft.Extensions.Logging if available, otherwise a debug logger.
	/// </summary>
	public IGeoLocatorLogger? Logger { get; set; }
}
