namespace Plugin.Maui.GeoLocator;

/// <summary>
/// A human-readable address produced by reverse geocoding.
/// </summary>
public sealed class GeoAddress
{
	public string? FeatureName { get; init; }

	public string? Thoroughfare { get; init; }

	public string? SubThoroughfare { get; init; }

	public string? Locality { get; init; }

	public string? SubLocality { get; init; }

	public string? AdminArea { get; init; }

	public string? SubAdminArea { get; init; }

	public string? PostalCode { get; init; }

	public string? CountryName { get; init; }

	public string? CountryCode { get; init; }

	/// <summary>
	/// Best-effort single-line representation of the address.
	/// </summary>
	public string? FormattedAddress { get; init; }

	public override string ToString() => FormattedAddress ?? FeatureName ?? string.Empty;
}
