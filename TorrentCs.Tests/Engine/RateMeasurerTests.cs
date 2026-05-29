using TorrentCs.Engine;

namespace TorrentCs.Tests.Engine;

public class RateMeasurerTests
{
    [Fact]
    public void AverageRate_NoMeasurements_ReturnsZero()
    {
        var measurer = new RateMeasurer();
        Assert.Equal(0, measurer.AverageRate());
    }

    [Fact]
    public void AverageRate_OneMeasurement_ReturnsZero()
    {
        var measurer = new RateMeasurer();
        measurer.AddMeasure(1000);
        Assert.Equal(0, measurer.AverageRate());
    }

    [Fact]
    public void AverageRate_TwoMeasurements_ReturnsNonZero()
    {
        var measurer = new RateMeasurer();
        measurer.AddMeasure(0);
        measurer.AddMeasure(30_000);
        Assert.True(measurer.AverageRate() >= 0);
    }

    [Fact]
    public void Reset_ClearsMeasurements()
    {
        var measurer = new RateMeasurer();
        measurer.AddMeasure(1000);
        measurer.AddMeasure(2000);
        measurer.Reset();
        Assert.Equal(0, measurer.AverageRate());
    }

    [Fact]
    public void AddMeasure_DoesNotThrow()
    {
        var measurer = new RateMeasurer();
        for (int i = 0; i < 100; i++)
            measurer.AddMeasure(i * 1024L);
    }
}
