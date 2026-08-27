namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Event data for a location update delivered while tracking.
/// </summary>
public sealed class LocationChangedEventArgs : EventArgs
{
	public LocationChangedEventArgs(GeoPosition location)
	{
		Location = location ?? throw new ArgumentNullException(nameof(location));
	}

	public GeoPosition Location { get; }
}
