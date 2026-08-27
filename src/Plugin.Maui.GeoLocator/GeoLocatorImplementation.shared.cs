using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace Plugin.Maui.GeoLocator;

partial class GeoLocatorImplementation : IGeoLocator
{
	IGeoLocatorLogger? _logger;

	public bool IsTracking { get; private set; }

	public bool IsLoggingEnabled { get; private set; }

	public event EventHandler<LocationChangedEventArgs>? LocationChanged;

	public event EventHandler<LocationErrorEventArgs>? LocationError;

	public void EnableLogging(bool enabled, IGeoLocatorLogger? logger = null)
	{
		IsLoggingEnabled = enabled;
		_logger = enabled ? logger ?? new DebugGeoLocatorLogger() : null;
		Log(GeoLocatorLogLevel.Information, enabled ? "Logging enabled." : "Logging disabled.");
	}

	public Task<PermissionStatus> CheckPermissionAsync() =>
		Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

	public Task<PermissionStatus> RequestPermissionAsync() =>
		MainThread.InvokeOnMainThreadAsync(Permissions.RequestAsync<Permissions.LocationWhenInUse>);

	async Task EnsurePermissionAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(true);
			if (status == PermissionStatus.Granted)
				return;

			if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
			{
				throw new GeoLocatorException(
					GeoLocatorError.PermissionDenied,
					"Location permission was denied. Enable it in Settings.");
			}

			status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>().ConfigureAwait(true);
			if (status != PermissionStatus.Granted)
			{
				throw new GeoLocatorException(
					GeoLocatorError.PermissionDenied,
					$"Location permission was not granted ({status}).");
			}
		}).ConfigureAwait(false);
	}

	void Log(GeoLocatorLogLevel level, string message, Exception? exception = null)
	{
		if (!IsLoggingEnabled)
			return;

		try
		{
			_logger?.Log(level, message, exception);
		}
		catch
		{
			// Logging must never break location operations.
		}
	}

	void RaiseLocationChanged(GeoPosition position)
	{
		Log(GeoLocatorLogLevel.Debug, $"Location updated: {position.Latitude:F6}, {position.Longitude:F6}");
		var args = new LocationChangedEventArgs(position);
		Dispatch(() => LocationChanged?.Invoke(this, args));
	}

	void RaiseLocationError(GeoLocatorError error, string message, Exception? exception = null)
	{
		Log(GeoLocatorLogLevel.Error, message, exception);
		var args = new LocationErrorEventArgs(error, message, exception);
		Dispatch(() => LocationError?.Invoke(this, args));
	}

	static void Dispatch(Action action)
	{
		if (MainThread.IsMainThread)
			action();
		else
			MainThread.BeginInvokeOnMainThread(action);
	}

	static void ValidateCoordinates(double latitude, double longitude)
	{
		if (latitude is < -90 or > 90)
			throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

		if (longitude is < -180 or > 180)
			throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
	}
}
