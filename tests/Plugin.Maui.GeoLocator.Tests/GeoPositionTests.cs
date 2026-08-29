namespace Plugin.Maui.GeoLocator.Tests;

public sealed class GeoPositionTests
{
    [Fact]
    public void Distance_between_the_same_point_is_zero()
    {
        var paris = Harness.Position(48.8566, 2.3522);

        Assert.Equal(0, GeoPosition.CalculateDistance(paris, paris), 6);
        Assert.Equal(0, GeoPosition.CalculateDistance(paris.Latitude, paris.Longitude, paris.Latitude, paris.Longitude), 6);
    }

    [Fact]
    public void Distance_paris_to_london_is_about_344_kilometers()
    {
        var paris = Harness.Position(48.8566, 2.3522);
        var london = Harness.Position(51.5074, -0.1278);

        var kilometers = GeoPosition.CalculateDistance(paris, london);

        Assert.InRange(kilometers, 340, 350);
    }

    [Fact]
    public void Distance_converts_to_miles_and_meters()
    {
        var from = Harness.Position(0, 0);
        var to = Harness.Position(0, 1);
        var kilometers = GeoPosition.CalculateDistance(from, to);
        var miles = GeoPosition.CalculateDistance(from, to, DistanceUnit.Miles);
        var meters = GeoPosition.CalculateDistance(from, to, DistanceUnit.Meters);

        Assert.InRange(kilometers, 110, 112);
        Assert.Equal(kilometers * 0.621371192, miles, 6);
        Assert.Equal(kilometers * 1000, meters, 6);
    }

    [Fact]
    public void Distance_rejects_null_positions()
    {
        var point = Harness.Position(0, 0);

        Assert.Throws<ArgumentNullException>(() => GeoPosition.CalculateDistance(null!, point));
        Assert.Throws<ArgumentNullException>(() => GeoPosition.CalculateDistance(point, null!));
    }

    [Fact]
    public void ToString_includes_coordinates_and_accuracy()
    {
        var position = new GeoPosition
        {
            Latitude = 12.345678,
            Longitude = -98.765432,
            Accuracy = 8,
            Timestamp = DateTimeOffset.Parse("2026-01-02T03:04:05Z")
        };

        var text = position.ToString();

        Assert.Contains("12.345678", text, StringComparison.Ordinal);
        Assert.Contains("-98.765432", text, StringComparison.Ordinal);
        Assert.Contains("8", text, StringComparison.Ordinal);
    }
}
