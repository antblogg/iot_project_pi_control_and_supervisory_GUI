namespace AspControllApp.Models;

public sealed class TemperatureAnalyticsViewModel
{
    public double CurrentTemperatureC { get; set; }
    public double AverageTemperatureC { get; set; }
    public double MinTemperatureC { get; set; }
    public double MaxTemperatureC { get; set; }
    public double TemperatureRangeC => MaxTemperatureC - MinTemperatureC;
    public double AverageRateOfChangeCPerSecond { get; set; }
    public int SampleCount { get; set; }
    public string LatestReadingText { get; set; } = "No data";

    public static TemperatureAnalyticsViewModel Empty() => new()
    {
        LatestReadingText = "No readings available"
    };
}
