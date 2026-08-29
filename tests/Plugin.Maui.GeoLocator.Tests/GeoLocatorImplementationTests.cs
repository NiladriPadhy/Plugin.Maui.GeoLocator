namespace Plugin.Maui.GeoLocator.Tests;

public sealed class GeoLocatorImplementationTests
{
    [Fact]
    public void Net_reference_assembly_reports_location_disabled()
    {
        var locator = Harness.CreateImplementation();

        Assert.False(locator.IsEnabled);
        Assert.False(locator.IsTracking);
        Assert.False(locator.IsLoggingEnabled);
    }

    [Fact]
    public async Task Location_apis_throw_feature_not_supported_on_net()
    {
        var locator = Harness.CreateImplementation();

        var last = await Assert.ThrowsAsync<GeoLocatorException>(() => locator.GetLastKnownLocationAsync());
        var current = await Assert.ThrowsAsync<GeoLocatorException>(() => locator.GetCurrentLocationAsync());
        var tracking = await Assert.ThrowsAsync<GeoLocatorException>(() => locator.StartTrackingAsync());
        var geocode = await Assert.ThrowsAsync<GeoLocatorException>(() =>
            locator.ReverseGeocodeAsync(47.6062, -122.3321));

        Assert.Equal(GeoLocatorError.FeatureNotSupported, last.Error);
        Assert.Equal(GeoLocatorError.FeatureNotSupported, current.Error);
        Assert.Equal(GeoLocatorError.FeatureNotSupported, tracking.Error);
        Assert.Equal(GeoLocatorError.FeatureNotSupported, geocode.Error);
        Assert.Contains("Android and iOS", last.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StopTracking_is_a_no_op_when_not_tracking()
    {
        var locator = Harness.CreateImplementation();

        await locator.StopTrackingAsync();

        Assert.False(locator.IsTracking);
    }

    [Fact]
    public void EnableLogging_records_through_the_supplied_logger()
    {
        var locator = Harness.CreateImplementation();
        var logger = new RecordingLogger();

        locator.EnableLogging(true, logger);

        Assert.True(locator.IsLoggingEnabled);
        Assert.Contains(logger.Entries, entry =>
            entry.Level == GeoLocatorLogLevel.Information &&
            entry.Message.Contains("enabled", StringComparison.OrdinalIgnoreCase));

        locator.EnableLogging(false);

        Assert.False(locator.IsLoggingEnabled);
    }

    [Fact]
    public void EnableLogging_swallows_logger_failures()
    {
        var locator = Harness.CreateImplementation();

        var exception = Record.Exception(() => locator.EnableLogging(true, new ThrowingLogger()));

        Assert.Null(exception);
        Assert.True(locator.IsLoggingEnabled);
    }

    [Fact]
    public void EnableLogging_without_logger_uses_debug_logger()
    {
        var locator = Harness.CreateImplementation();

        locator.EnableLogging(true);

        Assert.True(locator.IsLoggingEnabled);
    }
}
