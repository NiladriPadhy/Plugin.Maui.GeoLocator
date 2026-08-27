#pragma warning disable CA1416, CA1422
using Android.Content;
using Android.Locations;
using Android.OS;
using Java.Util.Functions;
using Microsoft.Maui.ApplicationModel;
using AndroidLocation = Android.Locations.Location;
using Address = Android.Locations.Address;

namespace Plugin.Maui.GeoLocator;

partial class GeoLocatorImplementation
{
	LocationManager? _manager;
	TrackingListener? _trackingListener;

	LocationManager Manager =>
		_manager ??= Platform.AppContext.GetSystemService(Context.LocationService) as LocationManager
			?? throw new GeoLocatorException(GeoLocatorError.Unavailable, "Location service is not available.");

	public bool IsEnabled
	{
		get
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(28))
				return Manager.IsLocationEnabled;

			return IsProviderEnabled(LocationManager.GpsProvider)
				|| IsProviderEnabled(LocationManager.NetworkProvider);
		}
	}

	public async Task<GeoPosition?> GetLastKnownLocationAsync(CancellationToken cancellationToken = default)
	{
		await EnsurePermissionAsync(cancellationToken).ConfigureAwait(false);
		EnsureLocationEnabled();
		Log(GeoLocatorLogLevel.Debug, "Getting last known location.");

		return await MainThread.InvokeOnMainThreadAsync(ReadBestLastKnownLocation).ConfigureAwait(false);
	}

	public async Task<GeoPosition?> GetCurrentLocationAsync(LocationRequest? request = null, CancellationToken cancellationToken = default)
	{
		request ??= new LocationRequest();
		await EnsurePermissionAsync(cancellationToken).ConfigureAwait(false);
		EnsureLocationEnabled();
		Log(GeoLocatorLogLevel.Information, $"Requesting current location (accuracy={request.Accuracy}, timeout={request.Timeout}).");

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutCts.CancelAfter(request.Timeout);

		try
		{
			var location = await GetCurrentLocationNativeAsync(request, timeoutCts.Token).ConfigureAwait(false);
			if (location is not null)
				return location;

			Log(GeoLocatorLogLevel.Warning, "Current location unavailable; falling back to last known location.");
			return ReadBestLastKnownLocation();
		}
		catch (System.OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			throw new GeoLocatorException(GeoLocatorError.Timeout, $"Timed out after {request.Timeout.TotalSeconds:0}s waiting for a location.");
		}
	}

	public async Task StartTrackingAsync(TrackingOptions? options = null, CancellationToken cancellationToken = default)
	{
		options ??= new TrackingOptions();
		await EnsurePermissionAsync(cancellationToken).ConfigureAwait(false);
		EnsureLocationEnabled();

		if (options.AllowBackgroundUpdates)
			Log(GeoLocatorLogLevel.Warning, "AllowBackgroundUpdates is set, but Android background tracking requires a host-app foreground service.");

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (IsTracking)
			{
				Log(GeoLocatorLogLevel.Warning, "Tracking is already running; restarting with new options.");
				StopTrackingCore();
			}

			var provider = ResolveProvider(options.Accuracy);
			_trackingListener = new TrackingListener(
				location => RaiseLocationChanged(location.ToGeoPosition()),
				providerName => RaiseLocationError(GeoLocatorError.LocationDisabled, $"Provider '{providerName}' was disabled."));

			var minTime = (long)Math.Max(0, options.MinimumTime.TotalMilliseconds);
			var minDistance = (float)Math.Max(0, options.MinimumDistanceMeters);

			Log(GeoLocatorLogLevel.Information, $"Starting tracking on '{provider}' (minTime={minTime}ms, minDistance={minDistance}m).");
			Manager.RequestLocationUpdates(provider, minTime, minDistance, _trackingListener, Looper.MainLooper);
			IsTracking = true;
		}).ConfigureAwait(false);
	}

	public Task StopTrackingAsync()
	{
		return MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (!IsTracking)
			{
				Log(GeoLocatorLogLevel.Debug, "Stop tracking ignored; tracking is not active.");
				return;
			}

			StopTrackingCore();
			Log(GeoLocatorLogLevel.Information, "Location tracking stopped.");
		});
	}

	public async Task<IReadOnlyList<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude, int maxResults = 5, CancellationToken cancellationToken = default)
	{
		ValidateCoordinates(latitude, longitude);
		maxResults = Math.Clamp(maxResults, 1, 20);
		Log(GeoLocatorLogLevel.Information, $"Reverse geocoding ({latitude}, {longitude}).");

		if (!Geocoder.IsPresent)
			throw new GeoLocatorException(GeoLocatorError.FeatureNotSupported, "Reverse geocoding is not available on this device.");

		cancellationToken.ThrowIfCancellationRequested();

		if (OperatingSystem.IsAndroidVersionAtLeast(33))
			return await ReverseGeocodeApi33Async(latitude, longitude, maxResults, cancellationToken).ConfigureAwait(false);

		return await Task.Run(() =>
		{
			cancellationToken.ThrowIfCancellationRequested();
			var geocoder = new Geocoder(Platform.AppContext, Java.Util.Locale.Default!);
#pragma warning disable CS0618
			var addresses = geocoder.GetFromLocation(latitude, longitude, maxResults);
#pragma warning restore CS0618
			return (IReadOnlyList<GeoAddress>)(addresses?.Select(static address => address.ToGeoAddress()).ToList() ?? []);
		}, cancellationToken).ConfigureAwait(false);
	}

	async Task<GeoPosition?> GetCurrentLocationNativeAsync(LocationRequest request, CancellationToken cancellationToken)
	{
		return await MainThread.InvokeOnMainThreadAsync(async () =>
		{
			var provider = ResolveProvider(request.Accuracy);
			Log(GeoLocatorLogLevel.Debug, $"Using provider '{provider}'.");

			if (OperatingSystem.IsAndroidVersionAtLeast(30))
				return await GetCurrentLocationApi30Async(provider, cancellationToken).ConfigureAwait(true);

			return await GetCurrentLocationLegacyAsync(provider, cancellationToken).ConfigureAwait(true);
		}).ConfigureAwait(false);
	}

	Task<GeoPosition?> GetCurrentLocationApi30Async(string provider, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<GeoPosition?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var signal = new CancellationSignal();
		cancellationToken.Register(() =>
		{
			signal.Cancel();
			tcs.TrySetCanceled(cancellationToken);
		});

		var executor = Platform.AppContext.MainExecutor
			?? throw new GeoLocatorException(GeoLocatorError.Unavailable, "Main executor is not available.");

		Manager.GetCurrentLocation(provider, signal, executor, new LocationConsumer(location =>
		{
			tcs.TrySetResult(location?.ToGeoPosition());
		}));

		return tcs.Task;
	}

	Task<GeoPosition?> GetCurrentLocationLegacyAsync(string provider, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<GeoPosition?>(TaskCreationOptions.RunContinuationsAsynchronously);
		SingleUpdateListener? listener = null;
		listener = new SingleUpdateListener(location =>
		{
			try
			{
				Manager.RemoveUpdates(listener!);
			}
			catch (Exception ex)
			{
				Log(GeoLocatorLogLevel.Warning, "Failed to remove single-update listener.", ex);
			}

			tcs.TrySetResult(location.ToGeoPosition());
		});

		cancellationToken.Register(() =>
		{
			try
			{
				Manager.RemoveUpdates(listener);
			}
			catch
			{
				// Best effort.
			}

			tcs.TrySetCanceled(cancellationToken);
		});

#pragma warning disable CS0618
		Manager.RequestSingleUpdate(provider, listener, Looper.MainLooper);
#pragma warning restore CS0618
		return tcs.Task;
	}

	Task<IReadOnlyList<GeoAddress>> ReverseGeocodeApi33Async(double latitude, double longitude, int maxResults, CancellationToken cancellationToken)
	{
		var tcs = new TaskCompletionSource<IReadOnlyList<GeoAddress>>(TaskCreationOptions.RunContinuationsAsynchronously);
		cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		MainThread.BeginInvokeOnMainThread(() =>
		{
			var geocoder = new Geocoder(Platform.AppContext, Java.Util.Locale.Default!);
			geocoder.GetFromLocation(latitude, longitude, maxResults, new GeocodeListener(
				addresses => tcs.TrySetResult(addresses.Select(static address => address.ToGeoAddress()).ToList()),
				error => tcs.TrySetException(new GeoLocatorException(GeoLocatorError.Unavailable, error ?? "Reverse geocoding failed."))));
		});

		return tcs.Task;
	}

	GeoPosition? ReadBestLastKnownLocation()
	{
		AndroidLocation? best = null;
		var providers = Manager.GetProviders(enabledOnly: true);
		if (providers is null)
			return null;

		foreach (var provider in providers)
		{
			AndroidLocation? last;
			try
			{
#pragma warning disable CS0618
				last = Manager.GetLastKnownLocation(provider);
#pragma warning restore CS0618
			}
			catch (Exception ex)
			{
				Log(GeoLocatorLogLevel.Warning, $"Last known location failed for {provider}.", ex);
				continue;
			}

			if (last is null)
				continue;

			if (best is null || last.Time > best.Time)
				best = last;
		}

		return best?.ToGeoPosition();
	}

	void StopTrackingCore()
	{
		if (_trackingListener is not null)
		{
			try
			{
				Manager.RemoveUpdates(_trackingListener);
			}
			catch (Exception ex)
			{
				Log(GeoLocatorLogLevel.Warning, "Failed to remove location updates.", ex);
			}

			_trackingListener.Dispose();
			_trackingListener = null;
		}

		IsTracking = false;
	}

	void EnsureLocationEnabled()
	{
		if (!IsEnabled)
			throw new GeoLocatorException(GeoLocatorError.LocationDisabled, "Location services are disabled on this device.");
	}

	bool IsProviderEnabled(string? provider) =>
		!string.IsNullOrEmpty(provider) && Manager.IsProviderEnabled(provider);

	string ResolveProvider(LocationAccuracy accuracy)
	{
		var preferGps = accuracy is LocationAccuracy.High or LocationAccuracy.Best or LocationAccuracy.BestForNavigation;

		if (preferGps && IsProviderEnabled(LocationManager.GpsProvider))
			return LocationManager.GpsProvider!;

		if (IsProviderEnabled(LocationManager.NetworkProvider))
			return LocationManager.NetworkProvider!;

		if (IsProviderEnabled(LocationManager.GpsProvider))
			return LocationManager.GpsProvider!;

		if (IsProviderEnabled(LocationManager.PassiveProvider))
			return LocationManager.PassiveProvider!;

		var criteria = new Criteria
		{
			Accuracy = preferGps ? Accuracy.Fine : Accuracy.Coarse,
			PowerRequirement = preferGps ? Power.High : Power.Medium
		};

		return Manager.GetBestProvider(criteria, enabledOnly: true)
			?? throw new GeoLocatorException(GeoLocatorError.Unavailable, "No enabled location provider is available.");
	}
}

sealed class LocationConsumer : Java.Lang.Object, IConsumer
{
	readonly Action<AndroidLocation?> _callback;

	public LocationConsumer(Action<AndroidLocation?> callback) => _callback = callback;

	public void Accept(Java.Lang.Object? value) => _callback(value as AndroidLocation);
}

sealed class SingleUpdateListener : Java.Lang.Object, ILocationListener
{
	readonly Action<AndroidLocation> _callback;

	public SingleUpdateListener(Action<AndroidLocation> callback) => _callback = callback;

	public void OnLocationChanged(AndroidLocation location) => _callback(location);

	public void OnProviderDisabled(string provider)
	{
	}

	public void OnProviderEnabled(string provider)
	{
	}

	public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
	{
	}
}

sealed class TrackingListener : Java.Lang.Object, ILocationListener
{
	readonly Action<AndroidLocation> _onLocation;
	readonly Action<string> _onDisabled;

	public TrackingListener(Action<AndroidLocation> onLocation, Action<string> onDisabled)
	{
		_onLocation = onLocation;
		_onDisabled = onDisabled;
	}

	public void OnLocationChanged(AndroidLocation location) => _onLocation(location);

	public void OnProviderDisabled(string provider) => _onDisabled(provider);

	public void OnProviderEnabled(string provider)
	{
	}

	public void OnStatusChanged(string? provider, Availability status, Bundle? extras)
	{
	}
}

sealed class GeocodeListener : Java.Lang.Object, Geocoder.IGeocodeListener
{
	readonly Action<IList<Address>> _onGeocode;
	readonly Action<string?> _onError;

	public GeocodeListener(Action<IList<Address>> onGeocode, Action<string?> onError)
	{
		_onGeocode = onGeocode;
		_onError = onError;
	}

	public void OnGeocode(IList<Address>? addresses) => _onGeocode(addresses ?? []);

	public void OnError(string? errorMessage) => _onError(errorMessage);
}

static class AndroidMappingExtensions
{
	public static GeoPosition ToGeoPosition(this AndroidLocation location)
	{
#pragma warning disable CS0618
		var isMock = location.IsFromMockProvider;
#pragma warning restore CS0618

		return new GeoPosition
		{
			Latitude = location.Latitude,
			Longitude = location.Longitude,
			Altitude = location.HasAltitude ? location.Altitude : null,
			Accuracy = location.HasAccuracy ? location.Accuracy : null,
			VerticalAccuracy = OperatingSystem.IsAndroidVersionAtLeast(26) && location.HasVerticalAccuracy
				? location.VerticalAccuracyMeters
				: null,
			Speed = location.HasSpeed ? location.Speed : null,
			Heading = location.HasBearing ? location.Bearing : null,
			Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(location.Time),
			IsFromMockProvider = isMock
		};
	}

	public static GeoAddress ToGeoAddress(this Address address)
	{
		var lines = new List<string>();
		for (var i = 0; i <= address.MaxAddressLineIndex; i++)
		{
			var line = address.GetAddressLine(i);
			if (!string.IsNullOrWhiteSpace(line))
				lines.Add(line);
		}

		return new GeoAddress
		{
			FeatureName = address.FeatureName,
			Thoroughfare = address.Thoroughfare,
			SubThoroughfare = address.SubThoroughfare,
			Locality = address.Locality,
			SubLocality = address.SubLocality,
			AdminArea = address.AdminArea,
			SubAdminArea = address.SubAdminArea,
			PostalCode = address.PostalCode,
			CountryName = address.CountryName,
			CountryCode = address.CountryCode,
			FormattedAddress = lines.Count > 0 ? string.Join(", ", lines) : address.FeatureName
		};
	}
}
#pragma warning restore CA1416, CA1422
