using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Services;

internal sealed class ServiceKqlRow : IKqlRow
{
    private readonly ServiceInfo _row;

    public ServiceKqlRow(KqlSession _, ServiceInfo row) => _row = row;

    public ServiceKqlRow(ServiceInfo row) => _row = row;

    public KqlValue Get(string canonical)
        => canonical switch
        {
            "SVC.Name" => KqlValue.From(_row.Name),
            "SVC.DisplayName" => Blank(_row.DisplayName),
            "SVC.Status" => KqlValue.From(_row.Status.ToString()),
            "SVC.StartType" => _row.StartType is { } start ? KqlValue.From(start.ToString()) : KqlValue.Unknown,
            "SVC.Pid" => _row.Pid is { } pid ? KqlValue.From(pid) : KqlValue.Unknown,
            "SVC.ImagePath" => Blank(_row.ImagePath),
            "SVC.Account" => Blank(_row.Account),
            "SVC.Kind" => KqlValue.From(_row.Kind.ToString()),
            "SVC.SharedProcess" => KqlValue.From(_row.SharedProcess),
            "SVC.Hidden" => KqlValue.From(_row.IsHidden),
            "SVC.DelayedAutoStart" => _row.DelayedAutoStart is { } delayed ? KqlValue.From(delayed) : KqlValue.Unknown,
            "SVC.TriggerStart" => _row.TriggerStart is { } trigger ? KqlValue.From(trigger) : KqlValue.Unknown,
            "SVC.LaunchProtected" => _row.LaunchProtected is { } launch ? KqlValue.From(launch.ToString()) : KqlValue.Unknown,
            "SVC.DependsOn" => _row.DependsOn.Count == 0 ? KqlValue.Unknown : KqlValue.From(string.Join(",", _row.DependsOn)),
            _ => KqlValue.Unknown
        };

    private static KqlValue Blank(string? value)
        => string.IsNullOrWhiteSpace(value) ? KqlValue.Unknown : KqlValue.From(value);
}
