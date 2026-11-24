using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AspControllApp.Models;

/// <summary>
/// MongoDB document model aligned with PI DAQ payload keys/formats (see pi/DAQ_main.py).
/// Temperature and timestamp values may be stored as strings when ingested as-is.
/// </summary>
public sealed class TemperatureReading
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Temperature")]
    public string? Temperature { get; set; }

    [BsonElement("timestamp")]
    public string? Timestamp { get; set; }

    [BsonElement("Setpoint")]
    public string? Setpoint { get; set; }

    [BsonElement("PWM_DutyCycle")]
    public string? PwmDutyCycle { get; set; }

    [BsonElement("Sensor")]
    public string? Sensor { get; set; }
}
