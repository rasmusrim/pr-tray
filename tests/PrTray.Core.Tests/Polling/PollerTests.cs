using PrTray.Core.Detection;
using PrTray.Core.GitHub;
using PrTray.Core.Polling;
using PrTray.Core.Storage;

namespace PrTray.Core.Tests.Polling;

public sealed class PollerTests : IDisposable
{
    private static readonly FixedTimeProvider Clock = new(TestPullRequests.Now);
    private static readonly string FixtureJson = Fixtures.Read("graphql-response.json");

    private readonly TemporaryDirectory temporaryDirectory = new();
    private readonly List<PollOutcome> outcomes = [];

    private SeenStore Store => new(temporaryDirectory.FilePath("seen.json"));

    public void Dispose() => temporaryDirectory.Dispose();

    private Poller CreatePoller(FakeProcessRunner runner)
    {
        var poller = new Poller(new GhClient(runner, Clock), Store, PrTrayConfig.Default, Clock);
        poller.Polled += outcomes.Add;
        return poller;
    }

    [Fact]
    public async Task First_poll_seeds_silently_and_later_new_approval_is_reported()
    {
        var withNewApproval = FixtureJson.Replace("\"R_A1\"", "\"R_A2\"");
        using var poller = CreatePoller(FakeProcessRunner.ReturningInOrder(
            FakeProcessRunner.Ok(FixtureJson),
            FakeProcessRunner.Ok(withNewApproval)));

        await poller.RefreshNowAsync();
        await poller.RefreshNowAsync();

        Assert.Empty(outcomes[0].Events);
        var prEvent = Assert.Single(outcomes[1].Events);
        Assert.Equal(PrEventKind.Approved, prEvent.Kind);
        Assert.Equal("PR_A", prEvent.PullRequest.Id);
        Assert.Contains("review:R_A2", Store.Load().Keys);
    }

    [Fact]
    public async Task First_poll_with_no_prs_still_ends_first_run()
    {
        const string emptyResponse = """{ "data": { "viewer": { "login": "rasmusrim" }, "mine": { "nodes": [] } } }""";
        using var poller = CreatePoller(FakeProcessRunner.Returning(FakeProcessRunner.Ok(emptyResponse)));

        await poller.RefreshNowAsync();

        Assert.False(Store.Load().IsFirstRun);
    }

    [Fact]
    public async Task Failed_fetch_reports_outcome_and_leaves_seen_state_untouched()
    {
        using var poller = CreatePoller(FakeProcessRunner.Returning(new ProcessResult(1, "", "HTTP 502")));

        await poller.RefreshNowAsync();

        Assert.IsType<GhResult.Failed>(Assert.Single(outcomes).Result);
        Assert.False(File.Exists(temporaryDirectory.FilePath("seen.json")));
    }

    [Fact]
    public async Task Concurrent_refreshes_run_gh_only_once()
    {
        var release = new TaskCompletionSource<ProcessResult>();
        var runner = new FakeProcessRunner((_, _) => release.Task);
        using var poller = CreatePoller(runner);

        var first = poller.RefreshNowAsync();
        var second = poller.RefreshNowAsync();
        await second;
        release.SetResult(FakeProcessRunner.Ok(FixtureJson));
        await first;

        Assert.Single(runner.Calls);
        Assert.Single(outcomes);
    }
}
