namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static InferenceResult InferDevice(IEnumerable<InferenceEvidence>? evidence)
        => DeviceInference.Score(evidence);
}
