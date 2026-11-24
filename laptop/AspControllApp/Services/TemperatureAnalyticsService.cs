using System.Globalization;
using AspControllApp.Models;
using AspControllApp.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AspControllApp.Services;

public sealed class TemperatureAnalyticsService
{
    private readonly MongoConnectionManager _connectionManager;
    private readonly IOptionsMonitor<MongoDbOptions> _optionsMonitor;

    public TemperatureAnalyticsService(MongoConnectionManager connectionManager, IOptionsMonitor<MongoDbOptions> optionsMonitor)
    {
        _connectionManager = connectionManager;
        _optionsMonitor = optionsMonitor;
    }

    public async Task<TemperatureAnalyticsViewModel> GetLastMinuteAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var collectionName = _optionsMonitor.CurrentValue.TemperatureCollectionName;
        var collection = _connectionManager.GetCollection<BsonDocument>(collectionName);
        if (collection is null)
        {
            return TemperatureAnalyticsViewModel.Empty();
        }

        var windowEnd = DateTime.UtcNow;
        var windowStart = windowEnd.AddMinutes(-1);

        var documents = await collection
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("_id"))
            .Limit(500)
            .ToListAsync(cancellationToken);

        var readings = documents
            .Select(TryReadReading)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .Where(r => r.Timestamp >= windowStart && r.Timestamp <= windowEnd)
            .OrderBy(r => r.Timestamp)
            .ToList();

        if (readings.Count == 0)
        {
            return TemperatureAnalyticsViewModel.Empty();
        }

        var temperatures = readings.Select(r => r.TemperatureC).ToList();
        var first = readings[0];
        var last = readings[^1];
        var elapsedSeconds = Math.Max((last.Timestamp - first.Timestamp).TotalSeconds, 1);

        return new TemperatureAnalyticsViewModel
        {
            CurrentTemperatureC = last.TemperatureC,
            AverageTemperatureC = temperatures.Average(),
            MinTemperatureC = temperatures.Min(),
            MaxTemperatureC = temperatures.Max(),
            AverageRateOfChangeCPerSecond = (last.TemperatureC - first.TemperatureC) / elapsedSeconds,
            SampleCount = readings.Count,
            LatestReadingText = FormatLatestReadingAge(windowEnd - last.Timestamp)
        };
    }

    private static (double TemperatureC, DateTime Timestamp)? TryReadReading(BsonDocument document)
    {
        if (!TryGetTemperature(document, out var temperatureC) || !TryGetTimestamp(document, out var timestamp))
        {
            return null;
        }

        return (temperatureC, timestamp);
    }

    private static bool TryGetTemperature(BsonDocument document, out double temperatureC)
    {
        temperatureC = default;

        if (!document.TryGetValue("Temperature", out var value) || value.IsBsonNull)
        {
            return false;
        }

        return value switch
        {
            BsonDouble bsonDouble => !double.IsNaN(bsonDouble.Value) && !double.IsInfinity(bsonDouble.Value) && (temperatureC = bsonDouble.Value) == bsonDouble.Value,
            BsonInt32 bsonInt32 => (temperatureC = bsonInt32.Value) == bsonInt32.Value,
            BsonInt64 bsonInt64 => (temperatureC = bsonInt64.Value) == bsonInt64.Value,
            BsonDecimal128 bsonDecimal => decimal.TryParse(bsonDecimal.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue)
                && (temperatureC = (double)decimalValue) is var _,
            BsonString bsonString => double.TryParse(bsonString.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out temperatureC)
                || double.TryParse(bsonString.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out temperatureC),
            _ => double.TryParse(value.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out temperatureC)
        };
    }

    private static bool TryGetTimestamp(BsonDocument document, out DateTime timestamp)
    {
        timestamp = default;

        if (!document.TryGetValue("timestamp", out var value) || value.IsBsonNull)
        {
            return false;
        }

        if (value.IsValidDateTime)
        {
            timestamp = value.ToUniversalTime();
            return true;
        }

        if (value.IsString)
        {
            if (DateTime.TryParse(value.AsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
            {
                return true;
            }

            if (DateTime.TryParse(value.AsString, CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatLatestReadingAge(TimeSpan age)
    {
        if (age.TotalSeconds < 1)
        {
            return "Just now";
        }

        var seconds = (int)Math.Round(age.TotalSeconds);
        return seconds == 1 ? "1 s ago" : $"{seconds} s ago";
    }
}
