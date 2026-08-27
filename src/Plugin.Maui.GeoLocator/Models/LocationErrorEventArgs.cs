namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Event data for a failure that occurs after a location operation has started.
/// </summary>
public sealed class LocationErrorEventArgs : EventArgs
{
	public LocationErrorEventArgs(GeoLocatorError error, string message, Exception? exception = null)
	{
		Error = error;
		Message = message ?? throw new ArgumentNullException(nameof(message));
		Exception = exception;
	}

	public GeoLocatorError Error { get; }

	public string Message { get; }

	public Exception? Exception { get; }
}
