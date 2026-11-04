using PrTray.Core.Detection;
using PrTray.Core.Models;
using PrTray.Core.Presentation;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Presentation;

public class UnreadCommentsTests
{
    private static readonly PullRequest MyPullRequest = Create(PrGroups.Mine) with { AuthorLogin = Me };

    private static PrEvent Commented(PullRequest pullRequest, params Comment[] comments) =>
        new(PrEventKind.Commented, pullRequest, comments[0].AuthorLogin) { Comments = comments };

    private static UnreadComments UnreadAfter(params Comment[] comments) =>
        UnreadComments.None.Add([Commented(MyPullRequest, comments)]);

    [Fact]
    public void Comment_events_make_the_pr_unread_from_its_first_new_comment()
    {
        var unread = UnreadAfter(CommentBy("colleague", id: "C_1"), CommentBy("teammate", id: "C_2"));

        var entry = Assert.Single(unread.Entries);
        Assert.Equal("PR_1", entry.PullRequestId);
        Assert.Equal("https://github.com/acme/widgets/pull/70", entry.PullRequestUrl);
        Assert.Equal("https://github.com/acme/widgets/pull/70#issuecomment-C_1", entry.FirstCommentUrl);
        Assert.Same(entry, unread.For(MyPullRequest));
    }

    [Fact]
    public void Other_events_make_nothing_unread()
    {
        var unread = UnreadComments.None.Add([new PrEvent(PrEventKind.Approved, MyPullRequest, "colleague")]);

        Assert.Equal(0, unread.Count);
        Assert.Null(unread.For(MyPullRequest));
    }

    [Fact]
    public void Later_comments_on_an_unread_pr_keep_the_first_unread_comment_and_move_the_latest_time()
    {
        var unread = UnreadAfter(CommentBy("colleague", id: "C_1"))
            .Add([Commented(MyPullRequest, CommentBy("colleague", id: "C_2") with { CreatedAt = Now })]);

        var entry = Assert.Single(unread.Entries);
        Assert.Equal("https://github.com/acme/widgets/pull/70#issuecomment-C_1", entry.FirstCommentUrl);
        Assert.Equal(Now, entry.LatestCommentAt);
    }

    [Theory]
    [InlineData("https://github.com/acme/widgets/pull/70")]
    [InlineData("https://github.com/acme/widgets/pull/70#issuecomment-C_9")]
    [InlineData("https://github.com/acme/widgets/pull/70/files")]
    public void Opening_the_pr_or_one_of_its_comments_marks_it_read(string openedUrl)
    {
        Assert.Equal(0, UnreadAfter(CommentBy("colleague")).MarkOpened(openedUrl).Count);
    }

    [Fact]
    public void Opening_a_different_pr_whose_url_starts_the_same_keeps_it_unread()
    {
        var unread = UnreadComments.None.Add([Commented(MyPullRequest with { Url = "https://github.com/acme/widgets/pull/7" }, CommentBy("colleague"))]);

        Assert.Equal(1, unread.MarkOpened("https://github.com/acme/widgets/pull/70").Count);
    }

    [Fact]
    public void Unread_comments_stay_while_the_pr_is_open_and_unanswered()
    {
        Assert.Equal(1, UnreadAfter(CommentBy("colleague")).KeepRelevant(Snapshot(MyPullRequest)).Count);
    }

    [Fact]
    public void Unread_comments_are_dropped_when_the_pr_is_no_longer_an_open_pr_of_mine()
    {
        var unread = UnreadAfter(CommentBy("colleague"));

        Assert.Equal(0, unread.KeepRelevant(Snapshot()).Count);
        Assert.Equal(0, unread.KeepRelevant(Snapshot(MyPullRequest with { State = PrState.Merged })).Count);
    }

    [Fact]
    public void Answering_on_github_after_the_latest_unread_comment_marks_the_pr_read()
    {
        var unread = UnreadAfter(CommentBy("colleague") with { CreatedAt = Now.AddMinutes(-10) });
        var answeredWithComment = MyPullRequest with { Comments = [CommentBy(Me, id: "C_2") with { CreatedAt = Now.AddMinutes(-5) }] };
        var answeredInReviewThread = MyPullRequest with { Reviews = [ReviewBy(Me, ReviewState.Commented) with { SubmittedAt = Now.AddMinutes(-5) }] };

        Assert.Equal(0, unread.KeepRelevant(Snapshot(answeredWithComment)).Count);
        Assert.Equal(0, unread.KeepRelevant(Snapshot(answeredInReviewThread)).Count);
    }

    [Fact]
    public void Answering_before_the_latest_unread_comment_keeps_the_pr_unread()
    {
        var unread = UnreadAfter(
            CommentBy("colleague", id: "C_1") with { CreatedAt = Now.AddMinutes(-20) },
            CommentBy("teammate", id: "C_3") with { CreatedAt = Now.AddMinutes(-5) });
        var answeredInBetween = MyPullRequest with { Comments = [CommentBy(Me, id: "C_2") with { CreatedAt = Now.AddMinutes(-10) }] };

        Assert.Equal(1, unread.KeepRelevant(Snapshot(answeredInBetween)).Count);
    }
}
