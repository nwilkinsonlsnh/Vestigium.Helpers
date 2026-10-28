using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class DeviceInferenceTests
{
    [Fact]
    public void Empty_evidence_is_unknown_with_no_confidence()
    {
        var result = NetworkHelper.InferDevice(null);
        Assert.Equal(InferredDeviceClass.Unknown, result.DeviceClass);
        Assert.Equal(InferredVendor.Unknown, result.Vendor);
        Assert.Equal(InferenceConfidence.None, result.Confidence);
        Assert.Empty(result.Evidence);
    }

    [Fact]
    public void Windows_port_cluster_infers_windows_host()
    {
        var evidence = new[]
        {
            new InferenceEvidence(InferenceEvidenceKind.PortOpen, "445", 445),
            new InferenceEvidence(InferenceEvidenceKind.PortOpen, "3389", 3389),
            new InferenceEvidence(InferenceEvidenceKind.PortOpen, "135", 135)
        };

        var result = NetworkHelper.InferDevice(evidence);
        Assert.Equal(InferredDeviceClass.WindowsHost, result.DeviceClass);
        Assert.Equal(InferredVendor.Unknown, result.Vendor);
        Assert.Equal(InferenceConfidence.High, result.Confidence);
        Assert.Equal(3, result.Evidence.Count);
    }

    [Fact]
    public void Snmp_and_oui_cisco_infers_network_device()
    {
        var evidence = new[]
        {
            new InferenceEvidence(InferenceEvidenceKind.PortOpen, "161", 161),
            new InferenceEvidence(InferenceEvidenceKind.SnmpSysDescr, "Cisco IOS Software, C9300"),
            new InferenceEvidence(InferenceEvidenceKind.OuiMatch, "Cisco Systems, Inc")
        };

        var result = NetworkHelper.InferDevice(evidence);
        Assert.Equal(InferredDeviceClass.NetworkDevice, result.DeviceClass);
        Assert.Equal(InferredVendor.Cisco, result.Vendor);
        Assert.Equal(InferenceConfidence.High, result.Confidence);
        Assert.Contains(result.Evidence, row => row.Kind == InferenceEvidenceKind.SnmpSysDescr);
    }

    [Fact]
    public void Single_ssh_port_is_low_confidence_unix()
    {
        var evidence = new[]
        {
            new InferenceEvidence(InferenceEvidenceKind.PortOpen, "22", 22)
        };

        var result = NetworkHelper.InferDevice(evidence);
        Assert.Equal(InferredDeviceClass.UnixLikeHost, result.DeviceClass);
        Assert.Equal(InferenceConfidence.Low, result.Confidence);
        Assert.Single(result.Evidence);
    }

    [Fact]
    public void Scorer_does_not_require_a_mac_or_a_trace()
    {
        var evidence = new[]
        {
            new InferenceEvidence(InferenceEvidenceKind.Banner, "SSH-2.0-OpenSSH_9.6")
        };

        var result = DeviceInference.Score(evidence);
        Assert.Equal(InferredDeviceClass.Unknown, result.DeviceClass);
        Assert.Equal(InferredVendor.Unknown, result.Vendor);
        Assert.Empty(result.Evidence);
    }
}
