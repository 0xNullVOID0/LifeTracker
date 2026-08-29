using System.Text.Json.Serialization;

namespace LifeTracker.Entities;

public abstract class ClimateMeasurement : BaseEntity
{
    public float Temperature { get; set; }
   
    public float Humidity { get; set; }
}
