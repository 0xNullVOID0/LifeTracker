using System.ComponentModel.DataAnnotations.Schema;

namespace LifeTracker.Entities.ESP32;

public class RoomClimateMeasurement : ClimateMeasurement
{
    public int ID { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public int CO2 { get; set; }
}
