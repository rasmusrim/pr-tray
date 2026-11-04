using PrTray.Core.Detection;
using PrTray.Core.Models;

namespace PrTray.Core.Presentation;

public sealed record UnreadComment(string PullRequestId, string PullRequestUrl, string FirstCommentUrl, DateTimeOffset? LatestCommentAt);

public sealed record UnreadComments(IReadOnlyList<UnreadComment> Entries)
{
    public static UnreadComments None { get; } = new([]);

    public int Count => Entries.Count;

    public UnreadComment? For(PullRequest pullRequest) =>
        Entries.FirstOrDefault(entry => entry.PullRequestId == pullRequest.Id);

    public UnreadComments Add(IEnumerable<PrEvent> events)
    {
        var entries = Entries.ToList();
        foreach (var prEvent in events.Where(prEvent => prEvent.Kind == PrEventKind.Commented && prEvent.Comments.Count > 0))
        {
            var latestCommentAt = prEvent.Comments.Max(comment => comment.CreatedAt);
            var index = entries.FindIndex(entry => entry.PullRequestId == prEvent.PullRequest.Id);
            if (index < 0)
                entries.Add(new UnreadComment(prEvent.PullRequest.Id, prEvent.PullRequest.Url, prEvent.Comments[0].Url, latestCommentAt));
            else if (latestCommentAt > entries[index].LatestCommentAt)
                entries[index] = entries[index] with { LatestCommentAt = latestCommentAt };
        }
        return new UnreadComments(entries);
    }

    // Unread comments stop mattering once the PR is no longer an open PR of mine, or once I have answered on GitHub.
    public UnreadComments KeepRelevant(PrSnapshot snapshot)
    {
        var myOpenPullRequests = snapshot.PullRequests
            .Where(pullRequest => pullRequest.State == PrState.Open && pullRequest.IsIn(PrGroups.Mine))
            .ToDictionary(pullRequest => pullRequest.Id);
        return new UnreadComments(Entries
            .Where(entry => myOpenPullRequests.TryGetValue(entry.PullRequestId, out var pullRequest)
                && !HasAnsweredSince(pullRequest, snapshot.ViewerLogin, entry.LatestCommentAt))
            .ToList());
    }

    public UnreadComments MarkOpened(string url) =>
        new(Entries.Where(entry => !IsWithinPullRequest(url, entry.PullRequestUrl)).ToList());

    private static bool HasAnsweredSince(PullRequest pullRequest, string viewerLogin, DateTimeOffset? since) =>
        pullRequest.Comments.Any(comment => comment.AuthorLogin == viewerLogin && comment.CreatedAt > since)
        || pullRequest.Reviews.Any(review => review.AuthorLogin == viewerLogin && review.SubmittedAt > since);

    // A plain prefix check would let ".../pull/7" match ".../pull/70".
    private static bool IsWithinPullRequest(string url, string pullRequestUrl) =>
        url == pullRequestUrl || url.StartsWith(pullRequestUrl + "#", StringComparison.Ordinal) || url.StartsWith(pullRequestUrl + "/", StringComparison.Ordinal);
}
