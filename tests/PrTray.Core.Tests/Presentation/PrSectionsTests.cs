using PrTray.Core.Models;
using PrTray.Core.Presentation;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Presentation;

public class PrSectionsTests
{
    [Fact]
    public void Splits_prs_into_sections_without_duplicates()
    {
        var mine = Create(PrGroups.Mine | PrGroups.Watched) with { Id = "MINE", AuthorLogin = Me };
        var requested = Create(PrGroups.ReviewRequested | PrGroups.Watched) with { Id = "REQUESTED" };
        var recommitted = Create(PrGroups.ReviewedByMe) with
        {
            Id = "RECOMMIT",
            Reviews = [ReviewBy(Me, ReviewState.ChangesRequested, commitOid: "old")],
        };
        var watched = Create(PrGroups.Watched) with { Id = "WATCHED" };
        var merged = Create(PrGroups.Mine) with { Id = "MERGED", State = PrState.Merged, MergedAt = Now };

        var sections = PrSections.From(Snapshot(mine, requested, recommitted, watched, merged));

        Assert.Equal(new[] { "MINE" }, sections.Mine.Select(pullRequest => pullRequest.Id));
        Assert.Equal(new[] { "RECOMMIT", "REQUESTED" }, sections.ToReview.Select(pullRequest => pullRequest.Id).Order());
        Assert.Equal(new[] { "WATCHED" }, sections.Watched.Select(pullRequest => pullRequest.Id));
        Assert.Equal(new[] { "MERGED" }, sections.RecentlyMerged.Select(pullRequest => pullRequest.Id));
    }

    [Fact]
    public void Open_prs_are_sorted_newest_first()
    {
        var older = Create(PrGroups.Watched) with { Id = "OLD", CreatedAt = Now.AddDays(-2) };
        var newer = Create(PrGroups.Watched) with { Id = "NEW", CreatedAt = Now.AddHours(-1) };

        var sections = PrSections.From(Snapshot(older, newer));

        Assert.Equal(new[] { "NEW", "OLD" }, sections.Watched.Select(pullRequest => pullRequest.Id));
    }

    [Fact]
    public void Watched_pr_with_new_commits_after_someone_elses_review_is_not_to_review_for_me()
    {
        var pullRequest = Create(PrGroups.Watched) with
        {
            Reviews = [ReviewBy("third-party", ReviewState.Commented, commitOid: "old")],
        };

        var sections = PrSections.From(Snapshot(pullRequest));

        Assert.Empty(sections.ToReview);
        Assert.Single(sections.Watched);
    }
}
