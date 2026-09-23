namespace PrTray.Core.Models;

public sealed record PullRequest(
    string Id,
    string Repository,
    int Number,
    string Title,
    string Url,
    PrState State,
    bool IsDraft,
    string AuthorLogin,
    DateTimeOffset CreatedAt,
    DateTimeOffset? MergedAt,
    ReviewDecision ReviewDecision,
    string? HeadCommitOid,
    DateTimeOffset? HeadCommittedAt,
    IReadOnlyList<Review> Reviews,
    PrGroups Groups)
{
    public string DisplayName => $"{Repository}#{Number}";

    public bool IsIn(PrGroups group) => (Groups & group) != 0;

    public Review? LatestHumanReviewByOthers() =>
        Reviews.LastOrDefault(review => !review.AuthorIsBot
            && review.AuthorLogin != AuthorLogin
            && review.State is ReviewState.Approved or ReviewState.ChangesRequested or ReviewState.Commented);

    public bool HasNewCommitsSinceUnapprovedReview()
    {
        var latestReview = LatestHumanReviewByOthers();
        return State == PrState.Open
            && latestReview is not null
            && latestReview.State != ReviewState.Approved
            && HeadCommitOid is not null
            && latestReview.CommitOid != HeadCommitOid;
    }
}
