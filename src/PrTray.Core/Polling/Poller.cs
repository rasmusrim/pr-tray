using PrTray.Core.Detection;
using PrTray.Core.GitHub;
using PrTray.Core.Storage;

namespace PrTray.Core.Polling;

public sealed class Poller(GhClient ghClient, SeenStore seenStore, PrTrayConfig config, TimeProvider timeProvider) : IDisposable
{
    private readonly SemaphoreSlim pollLock = new(1, 1);
    private readonly CancellationTokenSource stopping = new();
    private SeenState seenState = seenStore.Load();

    public event Action<PollOutcome>? Polled;

    public void Start() => _ = RunLoopAsync();

    public async Task RefreshNowAsync()
    {
        if (!await pollLock.WaitAsync(0))
            return;
        try
        {
            Polled?.Invoke(await PollOnceAsync());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Polled?.Invoke(new PollOutcome(new GhResult.Failed(exception.Message), [], timeProvider.GetUtcNow()));
        }
        finally
        {
            pollLock.Release();
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
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(config.PollIntervalSeconds), timeProvider);
            while (await timer.WaitForNextTickAsync(stopping.Token))
                await RefreshNowAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<PollOutcome> PollOnceAsync()
    {
        var result = await ghClient.FetchAsync(config, stopping.Token);
        if (result is not GhResult.Success success)
            return new PollOutcome(result, [], timeProvider.GetUtcNow());

        var detection = ChangeDetector.Detect(success.Snapshot, seenState, timeProvider.GetUtcNow());
        seenStore.Save(detection.SeenKeys);
        seenState = new SeenState(detection.SeenKeys, IsFirstRun: false);
        return new PollOutcome(result, detection.Events, timeProvider.GetUtcNow());
    }
}
