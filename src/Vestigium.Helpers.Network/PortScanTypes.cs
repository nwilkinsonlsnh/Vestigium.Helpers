public sealed record PortScanHostRow(
    string Host,
    IReadOnlyList<PortScanOpenPort> OpenTcp,
    IReadOnlyList<PortScanOpenPort> OpenUdp,
    InferenceResult? Inference,
    string? Mac = null);
