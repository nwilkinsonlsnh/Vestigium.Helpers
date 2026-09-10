using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class IcmpEchoStats
{
    public static void WriteIfRequested(string? path, IcmpEchoResult result)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;
        foreach (var reply in result.Replies)
        {
            CampaignJsonl.Append(path, new
            {
                kind = "echo",
                jobId = result.JobId,
                target = result.Target,
                sequence = reply.Sequence,
                status = reply.Status.ToString(),
                address = reply.Address,
                rttMs = reply.RoundtripTimeMs
            });
        }

        CampaignJsonl.Append(path, new
        {
            kind = "windowSummary",
            jobId = result.JobId,
            target = result.Target,
            sent = result.Sent,
            received = result.Received,
            lost = result.Lost,
            status = result.Status.ToString()
        });
        NetworkLog.Success(HelperLog.Subcategories.Stats, $"appended path={path} sent={result.Sent}");
    }
}
