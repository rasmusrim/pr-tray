using System.ComponentModel;
using PrTray.Core.GitHub;
using PrTray.Core.Storage;

namespace PrTray.Core.Tests.GitHub;

public class GhClientTests
{
    private static readonly FixedTimeProvider Clock = new(TestPullRequests.Now);

    private static Task<GhResult> FetchWith(FakeProcessRunner runner, TimeSpan? timeout = null) =>
        new GhClient(runner, Clock, timeout).FetchAsync(PrTrayConfig.Default, CancellationToken.None);

    [Fact]
    public async Task Success_parses_gh_output()
    {
        var result = await FetchWith(FakeProcessRunner.Returning(FakeProcessRunner.Ok(Fixtures.Read("graphql-response.json"))));

        var success = Assert.IsType<GhResult.Success>(result);
        Assert.Equal(4, success.Snapshot.PullRequests.Count);
    }

    [Fact]
    public async Task Calls_gh_api_graphql_with_query_including_watched_repositories()
    {
        var runner = FakeProcessRunner.Returning(FakeProcessRunner.Ok(Fixtures.Read("graphql-response.json")));

        await FetchWith(runner);

        var call = Assert.Single(runner.Calls);
        Assert.Equal("gh", call.FileName);
        Assert.Equal(new[] { "api", "graphql", "-f" }, call.Arguments.Take(3));
        Assert.StartsWith("query=query {", call.Arguments[3]);
        Assert.Contains("repo:acme/widgets", call.Arguments[3]);
        Assert.Contains("merged:>=2026-09-21", call.Arguments[3]);
    }

    [Fact]
    public async Task Exit_code_4_means_not_authenticated()
    {
        var result = await FetchWith(FakeProcessRunner.Returning(new ProcessResult(4, "", "To get started with GitHub CLI, please run:  gh auth login")));

        Assert.IsType<GhResult.NotAuthenticated>(result);
    }

    [Fact]
    public async Task Other_non_zero_exit_is_failed_with_first_stderr_line()
    {
        var result = await FetchWith(FakeProcessRunner.Returning(new ProcessResult(1, "", "HTTP 502: Bad Gateway\nmore details")));

        Assert.Equal(new GhResult.Failed("HTTP 502: Bad Gateway"), result);
    }

    [Fact]
    public async Task Missing_gh_executable_is_not_installed()
    {
        var runner = new FakeProcessRunner((_, _) => throw new Win32Exception(2, "No such file or directory"));

        Assert.IsType<GhResult.NotInstalled>(await FetchWith(runner));
    }

    [Fact]
    public async Task Hanging_gh_times_out_as_failed()
    {
        var runner = new FakeProcessRunner(async (_, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return FakeProcessRunner.Ok("");
        });

        var result = await FetchWith(runner, TimeSpan.FromMilliseconds(100));

        var failed = Assert.IsType<GhResult.Failed>(result);
        Assert.Contains("svarte ikke", failed.Message);
    }

    [Fact]
    public async Task Unparseable_output_is_failed()
    {
        var result = await FetchWith(FakeProcessRunner.Returning(FakeProcessRunner.Ok("not json")));

        var failed = Assert.IsType<GhResult.Failed>(result);
        Assert.StartsWith("Uventet svar fra gh", failed.Message);
    }

    [Fact]
    public async Task Existing_repository_is_found_with_gh_api()
    {
        var runner = FakeProcessRunner.Returning(FakeProcessRunner.Ok(""));

        var check = await new GhClient(runner, Clock).CheckRepositoryAsync(PrTrayConfig.Default, "rasmusrim/ku", CancellationToken.None);

        Assert.Equal(RepositoryCheck.Exists, check);
        Assert.Equal(new[] { "api", "repos/rasmusrim/ku", "--silent" }, Assert.Single(runner.Calls).Arguments);
    }

    [Fact]
    public async Task Http_404_means_repository_not_found()
    {
        var runner = FakeProcessRunner.Returning(new ProcessResult(1, "", "gh: Not Found (HTTP 404)"));

        var check = await new GhClient(runner, Clock).CheckRepositoryAsync(PrTrayConfig.Default, "rasmusrim/nope", CancellationToken.None);

        Assert.Equal(RepositoryCheck.NotFound, check);
    }

    [Fact]
    public async Task Other_failures_leave_the_repository_check_unknown()
    {
        var failing = FakeProcessRunner.Returning(new ProcessResult(1, "", "HTTP 502: Bad Gateway"));
        var missingGh = new FakeProcessRunner((_, _) => throw new Win32Exception(2, "No such file or directory"));

        Assert.Equal(RepositoryCheck.Unknown, await new GhClient(failing, Clock).CheckRepositoryAsync(PrTrayConfig.Default, "rasmusrim/ku", CancellationToken.None));
        Assert.Equal(RepositoryCheck.Unknown, await new GhClient(missingGh, Clock).CheckRepositoryAsync(PrTrayConfig.Default, "rasmusrim/ku", CancellationToken.None));
    }
}
