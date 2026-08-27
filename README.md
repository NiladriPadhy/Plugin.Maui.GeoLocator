# Plugin.Maui.GeoLocator

A .NET MAUI plugin for **Android** and **iOS** that provides:

- On-demand location retrieval
- Start / stop location tracking
- Reverse geocoding
- Optional logging

## Install

```bash
dotnet add package Plugin.Maui.GeoLocator
```

Or reference the project:

```xml
<ProjectReference Include="..\src\Plugin.Maui.GeoLocator\Plugin.Maui.GeoLocator.csproj" />
```

## Host app setup

### Android

Declare location permissions in `Platforms/Android/AndroidManifest.xml`:

```xml
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```

### iOS

Add usage descriptions to `Platforms/iOS/Info.plist`:

```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs your location to show where you are.</string>
```

For background tracking, also add always-authorization text and the `location` background mode.

## Register the plugin

```csharp
builder
    .UseMauiApp<App>()
    .UseGeoLocator(options =>
    {
        options.EnableLogging = true;
    });
```

Resolve `IGeoLocator` from dependency injection, or use `GeoLocator.Current`.

## Usage

### On-demand location

```csharp
var location = await GeoLocator.Current.GetCurrentLocationAsync(new LocationRequest
{
    Accuracy = LocationAccuracy.Best,
    Timeout = TimeSpan.FromSeconds(20)
});
```

Last cached fix:

```csharp
var last = await GeoLocator.Current.GetLastKnownLocationAsync();
```

### Start and stop tracking

```csharp
var locator = GeoLocator.Current;

locator.LocationChanged += (_, e) =>
{
    var position = e.Location;
};

locator.LocationError += (_, e) =>
{
    Console.WriteLine(e.Message);
};

await locator.StartTrackingAsync(new TrackingOptions
{
    Accuracy = LocationAccuracy.High,
    MinimumTime = TimeSpan.FromSeconds(2),
    MinimumDistanceMeters = 5
});

await locator.StopTrackingAsync();
```

### Reverse geocoding

```csharp
var addresses = await GeoLocator.Current.ReverseGeocodeAsync(
    latitude: 47.6062,
    longitude: -122.3321);

foreach (var address in addresses)
    Console.WriteLine(address.FormattedAddress);
```

### Logging

```csharp
GeoLocator.Current.EnableLogging(true);
GeoLocator.Current.EnableLogging(true, new DebugGeoLocatorLogger());
GeoLocator.Current.EnableLogging(false);
```

When `UseGeoLocator(options => options.EnableLogging = true)` is used, the plugin also writes through `ILogger` if the host app registered one (for example `builder.Logging.AddDebug()`).

## Sample

`samples/GeoLocator.Sample` is a MAUI app that exercises all four features. Deploy it to an Android emulator/device or an iOS simulator/device with location enabled.

```bash
dotnet build src/Plugin.Maui.GeoLocator/Plugin.Maui.GeoLocator.csproj
dotnet build samples/GeoLocator.Sample/GeoLocator.Sample.csproj -f net10.0-android
```

## Notes

- Tracking is designed for **foreground** use. Android background tracking requires a host-app foreground service. iOS background tracking requires `AllowBackgroundUpdates = true` plus the `location` background mode.
- The plugin requests **when-in-use** location permission at runtime.
- `net10.0` is included so shared code can reference the package. Location APIs throw `GeoLocatorException` (`FeatureNotSupported`) on that target.
