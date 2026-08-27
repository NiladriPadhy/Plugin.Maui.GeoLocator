using System.Diagnostics;

namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Writes plugin diagnostics to <see cref="Debug.WriteLine(string?)"/>.
/// </summary>
public sealed class DebugGeoLocatorLogger : IGeoLocatorLogger
{
	public void Log(GeoLocatorLogLevel level, string message, Exception? exception = null)
	{
		var line = exception is null
			? $"[GeoLocator] {level}: {message}"
			: $"[GeoLocator] {level}: {message}{Environment.NewLine}{exception}";

		Debug.WriteLine(line);
	}
}
