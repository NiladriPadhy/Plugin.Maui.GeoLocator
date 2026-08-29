#pragma warning disable CA1422
using CoreLocation;
using Foundation;
using Microsoft.Maui.ApplicationModel;

namespace Plugin.Maui.GeoLocator;

partial class GeoLocatorImplementation
{
	CLLocationManager? _manager;
	LocationManagerDelegate? _delegate;
	TaskCompletionSource<GeoPosition?>? _oneShotTcs;

	CLLocationManager Manager
	{
		get
		{
			if (_manager is not null)
				return _manager;

			if (!MainThread.IsMainThread)
				throw new InvalidOperationException("CLLocationManager must be created on the main thread.");

			_delegate = new LocationManagerDelegate();
			_delegate.LocationUpdated += OnNativeLocation;
			_delegate.FailedOccurred += OnNativeFailed;
			_delegate.AuthorizationStatusChanged += OnAuthorizationChanged;

			_manager = new CLLocationManager
			{
				Delegate = _delegate,
				PausesLocationUpdatesAutomatically = false
			};

			return _manager;
		}
	}

	public bool IsEnabled => CLLocationManager.LocationServicesEnabled;

	public Task<GeoPosition?> GetLastKnownLocationAsync(CancellationToken cancellationToken = default)
	{
		return MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await EnsurePermissionAsync(cancellationToken).ConfigureAwait(true);
			EnsureLocationEnabled();
			Log(GeoLocatorLogLevel.Debug, "Getting last known location.");
			return Manager.Location?.ToGeoPosition(IsReducedAccuracy());
		});
	}

	public async Task<GeoPosition?> GetCurrentLocationAsync(LocationRequest? request = null, CancellationToken cancellationToken = default)
	{
		request ??= new LocationRequest();

		using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutCts.CancelAfter(request.Timeout);

		try
		{
			return await MainThread.InvokeOnMainThreadAsync(() => GetCurrentLocationOnMainThreadAsync(request, timeoutCts.Token)).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			throw new GeoLocatorException(GeoLocatorError.Timeout, $"Timed out after {request.Timeout.TotalSeconds:0}s waiting for a location.");
		}
	}

	public Task StartTrackingAsync(TrackingOptions? options = null, CancellationToken cancellationToken = default)
	{
		options ??= new TrackingOptions();

		return MainThread.InvokeOnMainThreadAsync(async () =>
		{
			await EnsurePermissionAsync(cancellationToken).ConfigureAwait(true);
			EnsureLocationEnabled();
			_ = Manager;

			if (IsTracking)
			{
				Log(GeoLocatorLogLevel.Warning, "Tracking is already running; restarting with new options.");
				Manager.StopUpdatingLocation();
				Manager.StopUpdatingHeading();
			}

			ApplyAccuracy(Manager, options.Accuracy);
			Manager.DistanceFilter = Math.Max(0, options.MinimumDistanceMeters);
			Manager.AllowsBackgroundLocationUpdates = options.AllowBackgroundUpdates;

			Log(GeoLocatorLogLevel.Information, $"Starting tracking (accuracy={options.Accuracy}, distance={Manager.DistanceFilter}m).");
			Manager.StartUpdatingLocation();

			if (options.IncludeHeading)
				Manager.StartUpdatingHeading();

			IsTracking = true;
		});
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

			if (_manager is not null)
			{
				_manager.StopUpdatingLocation();
				_manager.StopUpdatingHeading();
			}

			IsTracking = false;
			Log(GeoLocatorLogLevel.Information, "Location tracking stopped.");
		});
	}

	public async Task<IReadOnlyList<GeoAddress>> ReverseGeocodeAsync(double latitude, double longitude, int maxResults = 5, CancellationToken cancellationToken = default)
	{
		ValidateCoordinates(latitude, longitude);
		maxResults = Math.Clamp(maxResults, 1, 20);
		Log(GeoLocatorLogLevel.Information, $"Reverse geocoding ({latitude}, {longitude}).");
		cancellationToken.ThrowIfCancellationRequested();

		var geocoder = new CLGeocoder();
		using var registration = cancellationToken.Register(geocoder.CancelGeocode);

		try
		{
			var placemarks = await geocoder.ReverseGeocodeLocationAsync(new CLLocation(latitude, longitude)).ConfigureAwait(false);
			if (placemarks is null)
				return [];

			return placemarks.Take(maxResults).Select(static placemark => placemark.ToGeoAddress()).ToList();
		}
		catch (Exception ex) when (ex is not OperationCanceledException and not GeoLocatorException)
		{
			throw new GeoLocatorException(GeoLocatorError.Unavailable, "Reverse geocoding failed.", ex);
		}
	}

	async Task<GeoPosition?> GetCurrentLocationOnMainThreadAsync(LocationRequest request, CancellationToken cancellationToken)
	{
		await EnsurePermissionAsync(cancellationToken).ConfigureAwait(true);
		EnsureLocationEnabled();
		_ = Manager;
		ApplyAccuracy(Manager, request.Accuracy);
		Log(GeoLocatorLogLevel.Information, $"Requesting current location (accuracy={request.Accuracy}).");

		if (_oneShotTcs is { } previous)
			previous.TrySetCanceled();

		var tcs = new TaskCompletionSource<GeoPosition?>(TaskCreationOptions.RunContinuationsAsynchronously);
		_oneShotTcs = tcs;

		using var registration = cancellationToken.Register(() =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				if (!IsTracking)
					_manager?.StopUpdatingLocation();
				tcs.TrySetCanceled(cancellationToken);
				if (ReferenceEquals(_oneShotTcs, tcs))
					_oneShotTcs = null;
			});
		});

		Manager.RequestLocation();
		return await tcs.Task.ConfigureAwait(true);
	}

	void EnsureLocationEnabled()
	{
		if (!IsEnabled)
			throw new GeoLocatorException(GeoLocatorError.LocationDisabled, "Location services are disabled on this device.");
	}

	void OnNativeLocation(CLLocation location)
	{
		var position = location.ToGeoPosition(IsReducedAccuracy());
		if (_oneShotTcs is { } oneShot)
		{
			_oneShotTcs = null;
			oneShot.TrySetResult(position);
		}

		if (IsTracking)
			RaiseLocationChanged(position);
	}

	void OnNativeFailed(NSError error)
	{
		var message = error.LocalizedDescription ?? "Location update failed.";
		Log(GeoLocatorLogLevel.Error, message);

		if (_oneShotTcs is { } oneShot)
		{
			_oneShotTcs = null;
			oneShot.TrySetException(new GeoLocatorException(GeoLocatorError.Unavailable, message));
			return;
		}

		RaiseLocationError(GeoLocatorError.Unavailable, message);
	}

	void OnAuthorizationChanged(CLAuthorizationStatus status)
	{
		Log(GeoLocatorLogLevel.Debug, $"Authorization changed: {status}");
		if (status is not (CLAuthorizationStatus.Denied or CLAuthorizationStatus.Restricted))
			return;

		if (_oneShotTcs is { } oneShot)
		{
			_oneShotTcs = null;
			oneShot.TrySetException(new GeoLocatorException(GeoLocatorError.PermissionDenied, "Location permission was denied."));
		}

		if (IsTracking)
		{
			_manager?.StopUpdatingLocation();
			_manager?.StopUpdatingHeading();
			IsTracking = false;
			RaiseLocationError(GeoLocatorError.PermissionDenied, "Location permission was denied.");
		}
	}

	bool IsReducedAccuracy()
	{
		if (!OperatingSystem.IsIOSVersionAtLeast(14) || _manager is null)
			return false;

		return _manager.AccuracyAuthorization == CLAccuracyAuthorization.ReducedAccuracy;
	}

	static void ApplyAccuracy(CLLocationManager manager, LocationAccuracy accuracy)
	{
		manager.DesiredAccuracy = accuracy switch
		{
			LocationAccuracy.Lowest => CLLocation.AccuracyThreeKilometers,
			LocationAccuracy.Low => CLLocation.AccuracyKilometer,
			LocationAccuracy.Medium => CLLocation.AccuracyHundredMeters,
			LocationAccuracy.High => CLLocation.AccuracyNearestTenMeters,
			LocationAccuracy.Best => CLLocation.AccuracyBest,
			LocationAccuracy.BestForNavigation => CLLocation.AccuracyBestForNavigation,
			_ => CLLocation.AccuracyHundredMeters
		};
	}
}

sealed class LocationManagerDelegate : CLLocationManagerDelegate
{
	public event Action<CLLocation>? LocationUpdated;

	public event Action<NSError>? FailedOccurred;

	public event Action<CLAuthorizationStatus>? AuthorizationStatusChanged;

	public override void LocationsUpdated(CLLocationManager manager, CLLocation[] locations)
	{
		if (locations.LastOrDefault() is { } location)
			LocationUpdated?.Invoke(location);
	}

	public override void Failed(CLLocationManager manager, NSError error) => FailedOccurred?.Invoke(error);

	public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status) =>
		AuthorizationStatusChanged?.Invoke(status);
}

static class IosMappingExtensions
{
	public static GeoPosition ToGeoPosition(this CLLocation location, bool reducedAccuracy)
	{
		return new GeoPosition
		{
			Latitude = location.Coordinate.Latitude,
			Longitude = location.Coordinate.Longitude,
			Altitude = location.VerticalAccuracy >= 0 ? location.Altitude : null,
			Accuracy = location.HorizontalAccuracy >= 0 ? location.HorizontalAccuracy : null,
			VerticalAccuracy = location.VerticalAccuracy >= 0 ? location.VerticalAccuracy : null,
			Speed = location.Speed >= 0 ? location.Speed : null,
			Heading = location.Course >= 0 ? location.Course : null,
			Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)(location.Timestamp.SecondsSince1970 * 1000)),
			ReducedAccuracy = reducedAccuracy
		};
	}

	public static GeoAddress ToGeoAddress(this CLPlacemark placemark)
	{
		var parts = new[]
		{
			placemark.SubThoroughfare,
			placemark.Thoroughfare,
			placemark.Locality,
			placemark.AdministrativeArea,
			placemark.PostalCode,
			placemark.Country
		}.Where(static part => !string.IsNullOrWhiteSpace(part));

		var formatted = string.Join(", ", parts);

		return new GeoAddress
		{
			FeatureName = placemark.Name,
			Thoroughfare = placemark.Thoroughfare,
			SubThoroughfare = placemark.SubThoroughfare,
			Locality = placemark.Locality,
			SubLocality = placemark.SubLocality,
			AdminArea = placemark.AdministrativeArea,
			SubAdminArea = placemark.SubAdministrativeArea,
			PostalCode = placemark.PostalCode,
			CountryName = placemark.Country,
			CountryCode = placemark.IsoCountryCode,
			FormattedAddress = string.IsNullOrWhiteSpace(formatted) ? placemark.Name : formatted
		};
	}
}
#pragma warning restore CA1422
