using PrTray.Core.GitHub;
using PrTray.Core.Models;

namespace PrTray.Core.Tests.GitHub;

public class GhResponseParserTests
{
    private static readonly PrSnapshot Snapshot =
        GhResponseParser.Parse(Fixtures.Read("graphql-response.json"), TestPullRequests.Now);

    private static PullRequest PullRequestWithId(string id) => Snapshot.PullRequests.Single(pullRequest => pullRequest.Id == id);

    [Fact]
    public void Parses_viewer_and_fetch_time()
    {
        Assert.Equal("rasmusrim", Snapshot.ViewerLogin);
        Assert.Equal(TestPullRequests.Now, Snapshot.FetchedAt);
    }

    [Fact]
    public void Merges_duplicates_across_aliases_and_skips_non_pr_nodes()
    {
        Assert.Equal(new[] { "PR_A", "PR_B", "PR_C", "PR_D" }, Snapshot.PullRequests.Select(pullRequest => pullRequest.Id).Order());
        Assert.Equal(PrGroups.ReviewedByMe | PrGroups.Watched, PullRequestWithId("PR_B").Groups);
        Assert.Equal(PrGroups.Mine | PrGroups.Watched, PullRequestWithId("PR_D").Groups);
    }

    [Fact]
    public void Parses_pull_request_fields()
    {
        var pullRequest = PullRequestWithId("PR_A");

        Assert.Equal("acme/widgets", pullRequest.Repository);
        Assert.Equal(72, pullRequest.Number);
        Assert.Equal("Legg til historikk", pullRequest.Title);
        Assert.Equal("https://github.com/acme/widgets/pull/72", pullRequest.Url);
        Assert.Equal(PrState.Open, pullRequest.State);
        Assert.Equal("rasmusrim", pullRequest.AuthorLogin);
        Assert.Equal(ReviewDecision.Approved, pullRequest.ReviewDecision);
        Assert.Equal("aaa111", pullRequest.HeadCommitOid);
        Assert.Equal(DateTimeOffset.Parse("2026-09-23T08:00:00Z"), pullRequest.HeadCommittedAt);
    }

    [Fact]
    public void Parses_reviews_in_order()
    {
        var reviews = PullRequestWithId("PR_B").Reviews;

        Assert.Equal(new[] { "R_B1", "R_B2" }, reviews.Select(review => review.Id));
        Assert.Equal(ReviewState.Commented, reviews[0].State);
        Assert.True(reviews[0].AuthorIsBot);
        Assert.Equal(ReviewState.ChangesRequested, reviews[1].State);
        Assert.False(reviews[1].AuthorIsBot);
        Assert.Equal("rasmusrim", reviews[1].AuthorLogin);
        Assert.Equal("bbb111", reviews[1].CommitOid);
        Assert.Equal(DateTimeOffset.Parse("2026-09-22T15:00:00Z"), reviews[1].SubmittedAt);
    }

    [Fact]
    public void Handles_deleted_author_missing_commits_and_null_review_decision()
    {
        var pullRequest = PullRequestWithId("PR_C");

        Assert.Equal("ghost", pullRequest.AuthorLogin);
        Assert.True(pullRequest.IsDraft);
        Assert.Null(pullRequest.HeadCommitOid);
        Assert.Null(pullRequest.HeadCommittedAt);
        Assert.Equal(ReviewDecision.None, pullRequest.ReviewDecision);
    }

    [Fact]
    public void Parses_merged_state()
    {
        var pullRequest = PullRequestWithId("PR_D");

        Assert.Equal(PrState.Merged, pullRequest.State);
        Assert.Equal(DateTimeOffset.Parse("2026-09-23T10:07:52Z"), pullRequest.MergedAt);
    }
}
