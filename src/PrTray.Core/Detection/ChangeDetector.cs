using PrTray.Core.Models;

namespace PrTray.Core.Detection;

public static class ChangeDetector
{
    public static readonly TimeSpan FreshnessWindow = TimeSpan.FromHours(24);

    private const PrGroups ApprovalAndMergeScope = PrGroups.Mine | PrGroups.ReviewedByMe | PrGroups.Watched;

    private const PrGroups NewCommitsScope = PrGroups.ReviewedByMe | PrGroups.Watched | PrGroups.ReviewRequested;

    private static readonly HashSet<PrEventKind> RedundantWithReviewRequest =
        [PrEventKind.Opened, PrEventKind.NewCommitsSinceUnapprovedReview];

    public static DetectionResult Detect(PrSnapshot snapshot, SeenState seen, DateTimeOffset now)
    {
        var seenKeys = new HashSet<string>(seen.Keys);
        var events = new List<PrEvent>();
        var candidates = snapshot.PullRequests.SelectMany(pullRequest => CandidatesFor(pullRequest, snapshot.ViewerLogin));
        foreach (var candidate in candidates)
        {
            var isUnseen = seenKeys.Add(candidate.Key);
            if (isUnseen && !seen.IsFirstRun && IsFresh(candidate.OccurredAt, now))
                events.Add(candidate.Event);
        }
        return new DetectionResult(SuppressRedundant(events), seenKeys);
    }

    private static IEnumerable<Candidate> CandidatesFor(PullRequest pullRequest, string viewerLogin)
    {
        if (pullRequest.State == PrState.Merged)
        {
            if (pullRequest.IsIn(ApprovalAndMergeScope))
                yield return new Candidate($"merged:{pullRequest.Id}", pullRequest.MergedAt, new PrEvent(PrEventKind.Merged, pullRequest, null));
            yield break;
        }

        if (pullRequest.IsIn(PrGroups.Watched) && pullRequest.AuthorLogin != viewerLogin)
            yield return new Candidate($"opened:{pullRequest.Id}", pullRequest.CreatedAt, new PrEvent(PrEventKind.Opened, pullRequest, pullRequest.AuthorLogin));

        if (pullRequest.IsIn(PrGroups.ReviewRequested))
            yield return new Candidate($"requested:{pullRequest.Id}:{pullRequest.HeadCommitOid}", null, new PrEvent(PrEventKind.ReviewRequested, pullRequest, pullRequest.AuthorLogin));

        if (pullRequest.AuthorLogin != viewerLogin && pullRequest.IsIn(NewCommitsScope) && pullRequest.HasNewCommitsSinceUnapprovedReview())
            yield return new Candidate($"recommit:{pullRequest.Id}:{pullRequest.HeadCommitOid}", pullRequest.HeadCommittedAt, new PrEvent(PrEventKind.NewCommitsSinceUnapprovedReview, pullRequest, pullRequest.AuthorLogin));

        foreach (var review in pullRequest.Reviews.Where(review => review.AuthorLogin != viewerLogin))
        {
            if (review.State == ReviewState.Approved && pullRequest.IsIn(ApprovalAndMergeScope))
                yield return new Candidate($"review:{review.Id}", review.SubmittedAt, new PrEvent(PrEventKind.Approved, pullRequest, review.AuthorLogin));

            if (review.State == ReviewState.ChangesRequested && pullRequest.IsIn(PrGroups.Mine))
                yield return new Candidate($"review:{review.Id}", review.SubmittedAt, new PrEvent(PrEventKind.ChangesRequested, pullRequest, review.AuthorLogin));
        }
    }

    private static bool IsFresh(DateTimeOffset? occurredAt, DateTimeOffset now) =>
        occurredAt is null || now - occurredAt.Value <= FreshnessWindow;

    private static IReadOnlyList<PrEvent> SuppressRedundant(List<PrEvent> events)
    {
        var reviewRequestedIds = events
            .Where(prEvent => prEvent.Kind == PrEventKind.ReviewRequested)
            .Select(prEvent => prEvent.PullRequest.Id)
            .ToHashSet();
        return events
            .Where(prEvent => !(RedundantWithReviewRequest.Contains(prEvent.Kind) && reviewRequestedIds.Contains(prEvent.PullRequest.Id)))
            .ToList();
    }

    private sealed record Candidate(string Key, DateTimeOffset? OccurredAt, PrEvent Event);
}
