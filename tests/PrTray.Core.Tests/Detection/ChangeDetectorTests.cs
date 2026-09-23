using PrTray.Core.Detection;
using PrTray.Core.Models;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Detection;

public class ChangeDetectorTests
{
    private static readonly SeenState NothingSeen = new(new HashSet<string>(), IsFirstRun: false);

    private static IReadOnlyList<PrEvent> EventsFor(params PullRequest[] pullRequests) =>
        ChangeDetector.Detect(Snapshot(pullRequests), NothingSeen, Now).Events;

    private static SeenState Seen(params string[] keys) => new(keys.ToHashSet(), IsFirstRun: false);

    [Fact]
    public void Opened_is_reported_for_new_pr_by_someone_else_in_watched_repo()
    {
        var prEvent = Assert.Single(EventsFor(Create(PrGroups.Watched)));

        Assert.Equal(PrEventKind.Opened, prEvent.Kind);
        Assert.Equal("colleague", prEvent.ActorLogin);
    }

    [Fact]
    public void Opened_is_not_reported_for_my_own_pr_in_watched_repo()
    {
        var pullRequest = Create(PrGroups.Watched | PrGroups.Mine) with { AuthorLogin = Me };

        Assert.Empty(EventsFor(pullRequest));
    }

    [Fact]
    public void Stale_events_are_not_reported_but_are_marked_seen()
    {
        var oldPullRequest = Create(PrGroups.Watched) with
        {
            CreatedAt = Now.AddDays(-3),
            Reviews = [ReviewBy("colleague", ReviewState.Approved) with { SubmittedAt = Now.AddDays(-2) }],
        };

        var result = ChangeDetector.Detect(Snapshot(oldPullRequest), NothingSeen, Now);

        Assert.Empty(result.Events);
        Assert.Contains("opened:PR_1", result.SeenKeys);
        Assert.Contains("review:R_1", result.SeenKeys);
    }

    [Fact]
    public void Approved_by_colleague_is_reported_on_my_pr()
    {
        var pullRequest = Create(PrGroups.Mine) with
        {
            AuthorLogin = Me,
            Reviews = [ReviewBy("colleague", ReviewState.Approved)],
        };

        var prEvent = Assert.Single(EventsFor(pullRequest));

        Assert.Equal(PrEventKind.Approved, prEvent.Kind);
        Assert.Equal("colleague", prEvent.ActorLogin);
    }

    [Fact]
    public void Approved_is_reported_on_watched_pr_that_is_not_mine()
    {
        var pullRequest = Create(PrGroups.Watched) with { Reviews = [ReviewBy("reviewer", ReviewState.Approved)] };

        var events = ChangeDetector.Detect(Snapshot(pullRequest), Seen("opened:PR_1"), Now).Events;

        Assert.Equal(PrEventKind.Approved, Assert.Single(events).Kind);
    }

    [Fact]
    public void My_own_approval_is_not_reported()
    {
        var pullRequest = Create(PrGroups.ReviewedByMe) with { Reviews = [ReviewBy(Me, ReviewState.Approved)] };

        Assert.Empty(EventsFor(pullRequest));
    }

    [Fact]
    public void Changes_requested_is_reported_on_my_pr()
    {
        var pullRequest = Create(PrGroups.Mine) with
        {
            AuthorLogin = Me,
            Reviews = [ReviewBy("colleague", ReviewState.ChangesRequested)],
        };

        var prEvent = Assert.Single(EventsFor(pullRequest));

        Assert.Equal(PrEventKind.ChangesRequested, prEvent.Kind);
    }

    [Fact]
    public void Changes_requested_is_not_reported_on_pr_that_is_not_mine()
    {
        var pullRequest = Create(PrGroups.Watched) with { Reviews = [ReviewBy("reviewer", ReviewState.ChangesRequested)] };

        var events = ChangeDetector.Detect(Snapshot(pullRequest), Seen("opened:PR_1"), Now).Events;

        Assert.Empty(events);
    }

    [Fact]
    public void Commented_reviews_are_ignored()
    {
        var pullRequest = Create(PrGroups.Mine) with
        {
            AuthorLogin = Me,
            Reviews = [ReviewBy("colleague", ReviewState.Commented)],
        };

        Assert.Empty(EventsFor(pullRequest));
    }

    [Fact]
    public void Review_request_is_reported()
    {
        var prEvent = Assert.Single(EventsFor(Create(PrGroups.ReviewRequested)));

        Assert.Equal(PrEventKind.ReviewRequested, prEvent.Kind);
    }

    [Fact]
    public void Review_request_is_reported_again_after_new_commits()
    {
        var events = ChangeDetector.Detect(Snapshot(Create(PrGroups.ReviewRequested)), Seen("requested:PR_1:old"), Now).Events;

        Assert.Equal(PrEventKind.ReviewRequested, Assert.Single(events).Kind);
    }

    [Fact]
    public void Opened_is_suppressed_when_the_same_pr_also_requests_my_review()
    {
        var result = ChangeDetector.Detect(Snapshot(Create(PrGroups.Watched | PrGroups.ReviewRequested)), NothingSeen, Now);

        Assert.Equal(PrEventKind.ReviewRequested, Assert.Single(result.Events).Kind);
        Assert.Contains("opened:PR_1", result.SeenKeys);
        Assert.Contains("requested:PR_1:head1", result.SeenKeys);
    }

    [Fact]
    public void New_commits_after_my_changes_request_are_reported()
    {
        var pullRequest = Create(PrGroups.ReviewedByMe) with
        {
            Reviews = [ReviewBy(Me, ReviewState.ChangesRequested, commitOid: "old")],
        };

        var prEvent = Assert.Single(EventsFor(pullRequest));

        Assert.Equal(PrEventKind.NewCommitsSinceUnapprovedReview, prEvent.Kind);
    }

    [Fact]
    public void New_commits_after_someone_elses_comment_review_are_reported_in_watched_repo()
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews = [ReviewBy("reviewer", ReviewState.Commented, commitOid: "old")],
        };

        var events = ChangeDetector.Detect(Snapshot(pullRequest), Seen("opened:PR_1"), Now).Events;

        Assert.Equal(PrEventKind.NewCommitsSinceUnapprovedReview, Assert.Single(events).Kind);
    }

    [Fact]
    public void New_commits_on_my_own_pr_are_not_reported()
    {
        var pullRequest = Create(PrGroups.Mine | PrGroups.Watched) with
        {
            AuthorLogin = Me,
            Reviews = [ReviewBy("reviewer", ReviewState.Commented, commitOid: "old")],
        };

        Assert.Empty(EventsFor(pullRequest));
    }

    [Fact]
    public void New_commits_are_suppressed_when_the_same_pr_also_requests_my_review()
    {
        var pullRequest = Create(PrGroups.ReviewedByMe | PrGroups.ReviewRequested) with
        {
            Reviews = [ReviewBy(Me, ReviewState.ChangesRequested, commitOid: "old")],
        };

        Assert.Equal(PrEventKind.ReviewRequested, Assert.Single(EventsFor(pullRequest)).Kind);
    }

    [Fact]
    public void Merged_pr_reports_only_merged()
    {
        var pullRequest = Create(PrGroups.Mine) with
        {
            AuthorLogin = Me,
            State = PrState.Merged,
            MergedAt = Now.AddMinutes(-5),
            Reviews = [ReviewBy("colleague", ReviewState.Approved)],
        };

        Assert.Equal(PrEventKind.Merged, Assert.Single(EventsFor(pullRequest)).Kind);
    }

    [Fact]
    public void First_run_records_keys_without_reporting()
    {
        var pullRequest = Create(PrGroups.Watched) with { Reviews = [ReviewBy("reviewer", ReviewState.Approved)] };

        var result = ChangeDetector.Detect(Snapshot(pullRequest), SeenState.FirstRun, Now);

        Assert.Empty(result.Events);
        Assert.Equal(new[] { "opened:PR_1", "review:R_1" }, result.SeenKeys.Order());
    }

    [Fact]
    public void Already_seen_keys_are_not_reported_again()
    {
        var pullRequest = Create(PrGroups.Watched) with { Reviews = [ReviewBy("reviewer", ReviewState.Approved)] };

        var events = ChangeDetector.Detect(Snapshot(pullRequest), Seen("opened:PR_1", "review:R_1"), Now).Events;

        Assert.Empty(events);
    }
}
