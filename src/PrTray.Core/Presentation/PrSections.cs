using PrTray.Core.Models;

namespace PrTray.Core.Presentation;

public sealed record PrSections(
    IReadOnlyList<PullRequest> Mine,
    IReadOnlyList<PullRequest> ToReview,
    IReadOnlyList<PullRequest> Watched,
    IReadOnlyList<PullRequest> RecentlyMerged)
{
    public static PrSections From(PrSnapshot snapshot)
    {
        var open = snapshot.PullRequests
            .Where(pullRequest => pullRequest.State == PrState.Open)
            .OrderByDescending(pullRequest => pullRequest.CreatedAt)
            .ToList();
        var mine = open.Where(pullRequest => pullRequest.IsIn(PrGroups.Mine)).ToList();
        var toReview = open
            .Where(pullRequest => !pullRequest.IsIn(PrGroups.Mine))
            .Where(pullRequest => pullRequest.IsIn(PrGroups.ReviewRequested)
                || pullRequest.HasNewCommitsSinceUnapprovedReview())
            .ToList();
        var watched = open
            .Where(pullRequest => pullRequest.IsIn(PrGroups.Watched))
            .Except(mine)
            .Except(toReview)
            .ToList();
        var recentlyMerged = snapshot.PullRequests
            .Where(pullRequest => pullRequest.State == PrState.Merged)
            .OrderByDescending(pullRequest => pullRequest.MergedAt)
            .ToList();
        return new PrSections(mine, toReview, watched, recentlyMerged);
    }
}
