using PrTray.Core.Detection;
using PrTray.Core.GitHub;
using PrTray.Core.Storage;

namespace PrTray.Core.Polling;

public sealed class Poller(GhClient ghClient, SeenStore seenStore, PrTrayConfig config, TimeProvider timeProvider) : IDisposable
{
    private readonly SemaphoreSlim pollLock = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private SeenState seenState = seenStore.Load();
    private volatile PrTrayConfig currentConfig = config;
    private bool refreshRequested;

    public event Action<PollOutcome>? Polled;

    public void Start() => _ = RunLoopAsync();

    public void UpdateConfig(PrTrayConfig newConfig) => currentConfig = newConfig;

    public async Task RefreshNowAsync()
    {
        Volatile.Write(ref refreshRequested, true);
        // Re-check after releasing: a request that arrives just before Release would otherwise be lost.
        while (Volatile.Read(ref refreshRequested) && await pollLock.WaitAsync(0))
        {
            try
            {
                while (Interlocked.Exchange(ref refreshRequested, false))
                    Polled?.Invoke(await PollOnceSafelyAsync());
            }
            finally
            {
                pollLock.Release();
            }
        }
    }

    public void Dispose()
    {
        stopping.Cancel();
        stopping.Dispose();
    }

    private async Task RunLoopAsync()
    {
        try
        {
            await RefreshNowAsync();
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(currentConfig.PollIntervalSeconds), timeProvider);
            while (await timer.WaitForNextTickAsync(stopping.Token))
                await RefreshNowAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<PollOutcome> PollOnceSafelyAsync()
    {
        try
        {
            return await PollOnceAsync();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new PollOutcome(new GhResult.Failed(exception.Message), [], timeProvider.GetUtcNow());
        }
    }

    private async Task<PollOutcome> PollOnceAsync()
    {
        var result = await ghClient.FetchAsync(currentConfig, stopping.Token);
        if (result is not GhResult.Success success)
            return new PollOutcome(result, [], timeProvider.GetUtcNow());

        var detection = ChangeDetector.Detect(success.Snapshot, seenState, timeProvider.GetUtcNow());
        seenStore.Save(detection.SeenKeys);
        seenState = new SeenState(detection.SeenKeys, IsFirstRun: false);
        return new PollOutcome(result, detection.Events, timeProvider.GetUtcNow());
    }
}
