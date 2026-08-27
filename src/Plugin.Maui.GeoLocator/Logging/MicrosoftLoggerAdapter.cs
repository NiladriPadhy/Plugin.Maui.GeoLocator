using Microsoft.Extensions.Logging;

namespace Plugin.Maui.GeoLocator;

sealed class MicrosoftLoggerAdapter(ILogger logger) : IGeoLocatorLogger
{
	public void Log(GeoLocatorLogLevel level, string message, Exception? exception = null)
	{
		logger.Log(ToLogLevel(level), exception, "{Message}", message);
	}

	static LogLevel ToLogLevel(GeoLocatorLogLevel level) => level switch
	{
		GeoLocatorLogLevel.Trace => LogLevel.Trace,
		GeoLocatorLogLevel.Debug => LogLevel.Debug,
		GeoLocatorLogLevel.Information => LogLevel.Information,
		GeoLocatorLogLevel.Warning => LogLevel.Warning,
		GeoLocatorLogLevel.Error => LogLevel.Error,
		_ => LogLevel.Information
	};
}
