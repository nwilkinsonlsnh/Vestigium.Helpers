namespace Vestigium.Helpers.Services;

public interface IServiceWatcher : IDisposable
{
    event EventHandler<IReadOnlyList<ServiceInfo>>? Sampled;
    TimeSpan Interval { get; }
}

internal sealed class ServiceWatcher : IServiceWatcher
{
    private readonly string? _name;
    private readonly string? _query;
    private readonly ServiceWatchFields _fields;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    public ServiceWatcher(string? name, string? query, TimeSpan interval, ServiceWatchFields fields)
    {
        _name = name;
        _query = query;
        _fields = fields;
        Interval = interval;
        _loop = Task.Run(RunAsync);
    }

    public event EventHandler<IReadOnlyList<ServiceInfo>>? Sampled;

    public TimeSpan Interval { get; }

    public void Dispose()
    {
        _cts.Cancel();
        try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
    }

    private async Task RunAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<ServiceInfo> rows;
                if (_name is not null)
                {
                    var one = ServiceHelper.Get(_name, ServiceDetailLevel.Slim, joinProcess: _fields.HasFlag(ServiceWatchFields.Process));
                    rows = one is null ? [] : [one];
                }
                else if (_query is not null)
                {
                    rows = ServiceHelper.Search(_query, ServiceDetailLevel.Slim, ServiceListScope.Visible);
                }
                else
                {
                    rows = [];
                }

                Sampled?.Invoke(this, rows);
            }
            catch
            {
            }

            try
            {
                await Task.Delay(Interval, _cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
