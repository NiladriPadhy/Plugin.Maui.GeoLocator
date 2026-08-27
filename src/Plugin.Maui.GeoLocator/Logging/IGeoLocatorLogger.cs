namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Receives diagnostic messages from the GeoLocator plugin.
/// </summary>
public interface IGeoLocatorLogger
{
	void Log(GeoLocatorLogLevel level, string message, Exception? exception = null);
}
