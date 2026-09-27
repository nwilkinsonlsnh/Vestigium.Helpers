namespace Vestigium.Helpers.PerfMon;

/// <summary>Finished job. Sample list is frozen.</summary>
public sealed class SampleJobResult
{
    public SampleJobResult(SampleStatus status, IReadOnlyList<SampleRecord> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        Status = status;
        Samples = samples as SampleRecord[] ?? samples.ToArray();
    }

    public SampleStatus Status { get; }
    public IReadOnlyList<SampleRecord> Samples { get; }
}
