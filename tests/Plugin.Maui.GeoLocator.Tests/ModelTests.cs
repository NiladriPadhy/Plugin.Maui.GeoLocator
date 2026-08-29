namespace Plugin.Maui.GeoLocator.Tests;

public sealed class ModelTests
{
    [Fact]
    public void LocationRequest_defaults_to_medium_accuracy_and_30s_timeout()
    {
        var request = new LocationRequest();

        Assert.Equal(LocationAccuracy.Medium, request.Accuracy);
        Assert.Equal(TimeSpan.FromSeconds(30), request.Timeout);
    }

    [Fact]
    public void TrackingOptions_have_documented_defaults()
    {
        var options = new TrackingOptions();

        Assert.Equal(LocationAccuracy.Medium, options.Accuracy);
        Assert.Equal(TimeSpan.FromSeconds(1), options.MinimumTime);
        Assert.Equal(10, options.MinimumDistanceMeters);
        Assert.False(options.IncludeHeading);
        Assert.False(options.AllowBackgroundUpdates);
    }

    [Fact]
    public void GeoAddress_ToString_prefers_formatted_then_feature_name()
    {
        Assert.Equal("1 Main St", new GeoAddress { FormattedAddress = "1 Main St", FeatureName = "Hall" }.ToString());
        Assert.Equal("Hall", new GeoAddress { FeatureName = "Hall" }.ToString());
        Assert.Equal(string.Empty, new GeoAddress().ToString());
    }

    [Fact]
    public void LocationChangedEventArgs_requires_a_position()
    {
        var position = Harness.Position(0, 0);
        var args = new LocationChangedEventArgs(position);

        Assert.Same(position, args.Location);
        Assert.Throws<ArgumentNullException>(() => new LocationChangedEventArgs(null!));
    }

    [Fact]
    public void LocationErrorEventArgs_requires_a_message()
    {
        var inner = new InvalidOperationException("inner");
        var args = new LocationErrorEventArgs(GeoLocatorError.Timeout, "timed out", inner);

        Assert.Equal(GeoLocatorError.Timeout, args.Error);
        Assert.Equal("timed out", args.Message);
        Assert.Same(inner, args.Exception);
        Assert.Throws<ArgumentNullException>(() => new LocationErrorEventArgs(GeoLocatorError.Timeout, null!));
    }

    [Fact]
    public void GeoLocatorException_preserves_error_and_inner_exception()
    {
        var inner = new TimeoutException();
        var exception = new GeoLocatorException(GeoLocatorError.Timeout, "waited too long", inner);

        Assert.Equal(GeoLocatorError.Timeout, exception.Error);
        Assert.Equal("waited too long", exception.Message);
        Assert.Same(inner, exception.InnerException);
    }

    [Fact]
    public void GeoLocatorOptions_start_with_logging_off()
    {
        var options = new GeoLocatorOptions();

        Assert.False(options.EnableLogging);
        Assert.Null(options.Logger);
    }

    [Fact]
    public void Debug_logger_does_not_throw()
    {
        var logger = new DebugGeoLocatorLogger();

        var exception = Record.Exception(() =>
            logger.Log(GeoLocatorLogLevel.Error, "failed", new InvalidOperationException("boom")));

        Assert.Null(exception);
    }
}
