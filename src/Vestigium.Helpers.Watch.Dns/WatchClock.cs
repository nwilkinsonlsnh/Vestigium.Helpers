namespace Vestigium.Helpers.Watch.Dns;

public sealed class WatchClock
{
    public const int DefaultSeconds = 5;
    public const int StepSeconds = 5;
    public const int MaxSeconds = 180;

    private readonly CancellationTokenSource _delay = new();
    private readonly TaskCompletionSource _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private WatchClock(int seconds)
    {
        Seconds = seconds;
        _ = FinishAsync(seconds);
    }

    public int Seconds { get; }

    public Task Completion => _done.Task;

    public static bool TryCreate(int? seconds, out WatchClock? clock, out string? reject)
    {
        var value = seconds ?? DefaultSeconds;
        if (value < StepSeconds || value > MaxSeconds || value % StepSeconds != 0)
        {
            clock = null;
            reject = $"Duration must be {StepSeconds} to {MaxSeconds} on a step of {StepSeconds}.";
            return false;
        }

        clock = new WatchClock(value);
        reject = null;
        return true;
    }

    public void Stop()
        => _delay.Cancel();

    private async Task FinishAsync(int seconds)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), _delay.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        _done.TrySetResult();
    }
}
