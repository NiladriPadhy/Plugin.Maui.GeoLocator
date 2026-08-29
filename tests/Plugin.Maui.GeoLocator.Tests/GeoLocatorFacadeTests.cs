namespace Plugin.Maui.GeoLocator.Tests;

public sealed class GeoLocatorFacadeTests
{
    [Fact]
    public void Current_creates_a_shared_implementation()
    {
        var first = GeoLocator.Current;
        var second = GeoLocator.Current;

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task SetDefault_replaces_the_shared_instance()
    {
        var previous = GeoLocator.Current;
        var fake = new FakeGeoLocator
        {
            Current = Harness.Position(19.0760, 72.8777)
        };

        try
        {
            GeoLocator.SetDefault(fake);

            Assert.Same(fake, GeoLocator.Current);
            var position = await GeoLocator.Current.GetCurrentLocationAsync();
            Assert.Equal(19.0760, position!.Latitude);
            Assert.Equal(72.8777, position.Longitude);
        }
        finally
        {
            GeoLocator.SetDefault(previous);
        }
    }

    [Fact]
    public void SetDefault_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => GeoLocator.SetDefault(null!));
    }

    [Fact]
    public async Task Fake_tracking_raises_location_and_error()
    {
        var fake = new FakeGeoLocator();
        GeoPosition? seen = null;
        LocationErrorEventArgs? error = null;
        fake.LocationChanged += (_, e) => seen = e.Location;
        fake.LocationError += (_, e) => error = e;

        await fake.StartTrackingAsync();
        fake.RaiseLocation(Harness.Position(1, 2));
        fake.RaiseError(GeoLocatorError.LocationDisabled, "GPS off");
        await fake.StopTrackingAsync();

        Assert.False(fake.IsTracking);
        Assert.Equal(1, seen!.Latitude);
        Assert.Equal(GeoLocatorError.LocationDisabled, error!.Error);
        Assert.Equal("GPS off", error.Message);
    }
}
