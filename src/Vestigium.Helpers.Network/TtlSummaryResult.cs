namespace Vestigium.Helpers.Network;

public sealed record TtlSummaryResult(
    int SampleCount,
    int? MinTtl,
    int? MaxTtl,
    int? ModeTtl,
    int? EstimatedHops,
    int? AssumedInitialTtl);
