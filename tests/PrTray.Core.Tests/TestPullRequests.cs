using PrTray.Core.Models;

namespace PrTray.Core.Tests;

internal static class TestPullRequests
{
    public const string Me = "rasmusrim";
    public static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    public static PullRequest Create(PrGroups groups) => new(
        Id: "PR_1",
        Repository: "acme/widgets",
        Number: 70,
        Title: "Legg til historikk",
        Url: "https://github.com/acme/widgets/pull/70",
        State: PrState.Open,
        IsDraft: false,
        AuthorLogin: "colleague",
        CreatedAt: Now.AddHours(-1),
        MergedAt: null,
        ReviewDecision: ReviewDecision.ReviewRequired,
        HeadCommitOid: "head1",
        HeadCommittedAt: Now.AddMinutes(-30),
        Reviews: [],
        Groups: groups);

    public static Review ReviewBy(string author, ReviewState state, string id = "R_1", string commitOid = "head1") =>
        new(id, state, author, AuthorIsBot: false, Now.AddMinutes(-10), commitOid);

    public static PrSnapshot Snapshot(params PullRequest[] pullRequests) => new(Me, pullRequests, Now);
}
