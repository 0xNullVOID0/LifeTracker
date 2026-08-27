using System.ComponentModel;

﻿namespace LifeTracker.Entities.Garmin;

public class DailySleep : GarminEntity

{
    [DefaultValue(30780)]
    public int SleepTimeSeconds { get; set; }

    [DefaultValue(5640)]
    public int DeepSleepSeconds { get; set; }

    [DefaultValue(18360)]
    public int LightSleepSeconds { get; set; }

    [DefaultValue(6780)]
    public int RemSleepSeconds { get; set; }

    [DefaultValue(2040)]
    public int AwakeSleepSeconds { get; set; }

    // TODO just turn into ints and handle proper dto conversion 
    [DefaultValue(55.0)]
    public double? AvgHeartRate { get; set; }

    [DefaultValue(8.0)]
    public double? AvgSleepStress { get; set; }
}
