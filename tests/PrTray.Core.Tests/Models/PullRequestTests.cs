using PrTray.Core.Models;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Models;

public class PullRequestTests
{
    [Theory]
    [InlineData(ReviewState.ChangesRequested)]
    [InlineData(ReviewState.Commented)]
    public void Has_new_commits_when_latest_review_was_not_an_approval_on_an_older_commit(ReviewState state)
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews = [ReviewBy("reviewer", state, commitOid: "old")],
        };

        Assert.True(pullRequest.HasNewCommitsSinceUnapprovedReview());
    }

    [Fact]
    public void Has_no_new_commits_when_head_is_the_reviewed_commit()
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews = [ReviewBy("reviewer", ReviewState.ChangesRequested, commitOid: "head1")],
        };

        Assert.False(pullRequest.HasNewCommitsSinceUnapprovedReview());
    }

    [Fact]
    public void Has_no_new_commits_when_latest_review_is_an_approval()
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews =
            [
                ReviewBy("reviewer", ReviewState.ChangesRequested, id: "R_1", commitOid: "old"),
                ReviewBy("another", ReviewState.Approved, id: "R_2", commitOid: "middle"),
            ],
        };

        Assert.False(pullRequest.HasNewCommitsSinceUnapprovedReview());
    }

    [Fact]
    public void Bot_reviews_and_the_authors_own_reviews_are_ignored()
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews =
            [
                ReviewBy("reviewer", ReviewState.Approved, id: "R_1", commitOid: "old"),
                ReviewBy("copilot-pull-request-reviewer", ReviewState.Commented, id: "R_2", commitOid: "old") with { AuthorIsBot = true },
                ReviewBy("colleague", ReviewState.Commented, id: "R_3", commitOid: "old"),
            ],
        };

        Assert.False(pullRequest.HasNewCommitsSinceUnapprovedReview());
    }

    [Fact]
    public void Display_name_combines_repository_and_number()
    {
        Assert.Equal("acme/widgets#70", Create(PrGroups.Mine).DisplayName);
    }
}
