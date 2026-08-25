namespace LifeTracker.Entities;

public abstract class GarminEntity : BaseEntity
{
    public DateOnly Date { get; set; } // primary key(for most, HeartRateSample is a composite PK of date + timestamp)

}
