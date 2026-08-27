namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Thrown when a location or geocoding operation cannot be completed.
/// </summary>
public sealed class GeoLocatorException : Exception
{
	public GeoLocatorException(GeoLocatorError error, string message, Exception? innerException = null)
		: base(message, innerException)
	{
		Error = error;
	}

	public GeoLocatorError Error { get; }
}
