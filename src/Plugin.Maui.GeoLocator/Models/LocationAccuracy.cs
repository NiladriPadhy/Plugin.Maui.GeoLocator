namespace Plugin.Maui.GeoLocator;

/// <summary>
/// Desired accuracy for an on-demand location request or a tracking session.
/// </summary>
public enum LocationAccuracy
{
	/// <summary>City-level accuracy, typically around 3 km.</summary>
	Lowest,

	/// <summary>Neighborhood-level accuracy, typically around 1 km.</summary>
	Low,

	/// <summary>Block-level accuracy, typically around 100 m.</summary>
	Medium,

	/// <summary>Street-level accuracy, typically around 10 m.</summary>
	High,

	/// <summary>Best available accuracy from the platform.</summary>
	Best,

	/// <summary>Highest accuracy, intended for navigation. Uses more power.</summary>
	BestForNavigation
}
