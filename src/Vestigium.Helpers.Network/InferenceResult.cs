namespace Vestigium.Helpers.Network;

public sealed record InferenceResult(
    InferredDeviceClass DeviceClass,
    InferredVendor Vendor,
    InferenceConfidence Confidence,
    IReadOnlyList<InferenceEvidence> Evidence);
