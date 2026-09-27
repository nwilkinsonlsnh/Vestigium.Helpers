namespace Vestigium.Helpers.PerfMon;

/// <summary>Outcome of one sample or of a finished job.</summary>
public enum SampleStatus
{
    Ok = 0,
    Unavailable = 1,
    Partial = 2,
    Cancelled = 3,
    Rejected = 4
}
