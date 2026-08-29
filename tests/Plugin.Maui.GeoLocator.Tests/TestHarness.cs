namespace Plugin.Maui.GeoLocator.Tests;

static class Harness
{
    public static GeoLocatorImplementation CreateImplementation() => new();

    public static GeoPosition Position(double latitude, double longitude) => new()
    {
        Latitude = latitude,
        Longitude = longitude,
        Timestamp = DateTimeOffset.UnixEpoch
    };
}

sealed class RecordingLogger : IGeoLocatorLogger
{
    public List<(GeoLocatorLogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public void Log(GeoLocatorLogLevel level, string message, Exception? exception = null) =>
        Entries.Add((level, message, exception));
}

sealed class ThrowingLogger : IGeoLocatorLogger
{
    public void Log(GeoLocatorLogLevel level, string message, Exception? exception = null) =>
        throw new InvalidOperationException("logger failed");
}

sealed class FakeGeoLocator : IGeoLocator
{
    public bool IsEnabled { get; set; } = true;

    public bool IsTracking { get; private set; }

    public bool IsLoggingEnabled { get; private set; }

    public GeoPosition? LastKnown { get; set; }

    public GeoPosition? Current { get; set; }

    public IReadOnlyList<GeoAddress> Addresses { get; set; } = [];

    public event EventHandler<LocationChangedEventArgs>? LocationChanged;

    public event EventHandler<LocationErrorEventArgs>? LocationError;

    public void EnableLogging(bool enabled, IGeoLocatorLogger? logger = null) =>
        IsLoggingEnabled = enabled;

    public Task<PermissionStatus> CheckPermissionAsync() =>
        Task.FromResult(PermissionStatus.Granted);

    public Task<PermissionStatus> RequestPermissionAsync() =>
        Task.FromResult(PermissionStatus.Granted);

    public Task<GeoPosition?> GetLastKnownLocationAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(LastKnown);

    public Task<GeoPosition?> GetCurrentLocationAsync(LocationRequest? request = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Current);

    public Task StartTrackingAsync(TrackingOptions? options = null, CancellationToken cancellationToken = default)
    {
        IsTracking = true;
        return Task.CompletedTask;
    }

    public Task StopTrackingAsync()
    {
        IsTracking = false;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<GeoAddress>> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        int maxResults = 5,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Addresses);

    public void RaiseLocation(GeoPosition position) =>
        LocationChanged?.Invoke(this, new LocationChangedEventArgs(position));

    public void RaiseError(GeoLocatorError error, string message) =>
        LocationError?.Invoke(this, new LocationErrorEventArgs(error, message));
}
