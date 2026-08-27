using Plugin.Maui.GeoLocator;

namespace GeoLocator.Sample;

public partial class MainPage : ContentPage, IGeoLocatorLogger
{
	readonly IGeoLocator _geoLocator;
	readonly List<string> _logLines = [];
	GeoPosition? _lastPosition;

	public MainPage()
	{
		InitializeComponent();
		_geoLocator = Plugin.Maui.GeoLocator.GeoLocator.Current;
		_geoLocator.LocationChanged += OnLocationChanged;
		_geoLocator.LocationError += OnLocationError;
		_geoLocator.EnableLogging(true, this);
		ServicesLabel.Text = _geoLocator.IsEnabled ? "Services: on" : "Services: off";
	}

	async void OnGetCurrentLocationClicked(object? sender, EventArgs e)
	{
		await RunAsync("Getting current location...", async () =>
		{
			_lastPosition = await _geoLocator.GetCurrentLocationAsync(new LocationRequest
			{
				Accuracy = LocationAccuracy.Best,
				Timeout = TimeSpan.FromSeconds(20)
			});

			ShowPosition(_lastPosition);
		});
	}

	async void OnGetLastKnownClicked(object? sender, EventArgs e)
	{
		await RunAsync("Reading last known location...", async () =>
		{
			_lastPosition = await _geoLocator.GetLastKnownLocationAsync();
			ShowPosition(_lastPosition);
		});
	}

	async void OnStartTrackingClicked(object? sender, EventArgs e)
	{
		await RunAsync("Starting tracking...", async () =>
		{
			await _geoLocator.StartTrackingAsync(new TrackingOptions
			{
				Accuracy = LocationAccuracy.High,
				MinimumTime = TimeSpan.FromSeconds(2),
				MinimumDistanceMeters = 5
			});

			StatusLabel.Text = "Tracking...";
		});
	}

	async void OnStopTrackingClicked(object? sender, EventArgs e)
	{
		await RunAsync("Stopping tracking...", async () =>
		{
			await _geoLocator.StopTrackingAsync();
			StatusLabel.Text = "Tracking stopped.";
		});
	}

	async void OnReverseGeocodeClicked(object? sender, EventArgs e)
	{
		if (_lastPosition is null)
		{
			AddressLabel.Text = "Get a location first.";
			return;
		}

		await RunAsync("Reverse geocoding...", async () =>
		{
			var addresses = await _geoLocator.ReverseGeocodeAsync(_lastPosition.Latitude, _lastPosition.Longitude);
			AddressLabel.Text = addresses.Count == 0
				? "No addresses found."
				: string.Join(Environment.NewLine + Environment.NewLine, addresses.Select(address => address.ToString()));
		});
	}

	void OnLoggingToggled(object? sender, ToggledEventArgs e)
	{
		_geoLocator.EnableLogging(e.Value, this);
		AppendLog(e.Value ? "Logging enabled by user." : "Logging disabled by user.");
	}

	void OnLocationChanged(object? sender, LocationChangedEventArgs e)
	{
		_lastPosition = e.Location;
		ShowPosition(e.Location);
		StatusLabel.Text = "Tracking...";
	}

	void OnLocationError(object? sender, LocationErrorEventArgs e)
	{
		StatusLabel.Text = e.Message;
		AppendLog($"ERROR {e.Error}: {e.Message}");
	}

	void ShowPosition(GeoPosition? position)
	{
		LocationLabel.Text = position is null
			? "No location available."
			: $"Lat: {position.Latitude:F6}{Environment.NewLine}" +
			  $"Lon: {position.Longitude:F6}{Environment.NewLine}" +
			  $"Accuracy: {position.Accuracy?.ToString("0") ?? "n/a"} m{Environment.NewLine}" +
			  $"Altitude: {position.Altitude?.ToString("0.0") ?? "n/a"} m{Environment.NewLine}" +
			  $"Speed: {position.Speed?.ToString("0.0") ?? "n/a"} m/s{Environment.NewLine}" +
			  $"Time: {position.Timestamp:u}";
	}

	async Task RunAsync(string status, Func<Task> operation)
	{
		try
		{
			StatusLabel.Text = status;
			await operation();
			if (StatusLabel.Text == status)
				StatusLabel.Text = "Done.";
		}
		catch (Exception ex)
		{
			StatusLabel.Text = ex.Message;
			AppendLog(ex.ToString());
		}
	}

	public void Log(GeoLocatorLogLevel level, string message, Exception? exception = null)
	{
		var line = exception is null
			? $"{DateTime.Now:HH:mm:ss} {level}: {message}"
			: $"{DateTime.Now:HH:mm:ss} {level}: {message} ({exception.GetType().Name})";

		MainThread.BeginInvokeOnMainThread(() => AppendLog(line));
	}

	void AppendLog(string line)
	{
		_logLines.Insert(0, line);
		if (_logLines.Count > 40)
			_logLines.RemoveAt(_logLines.Count - 1);

		LogLabel.Text = string.Join(Environment.NewLine, _logLines);
	}
}
