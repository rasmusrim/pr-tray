using PrTray.Core.GitHub;
using PrTray.Core.Models;
using PrTray.Core.Presentation;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Presentation;

public class TrayStatusCalculatorTests
{
    private static TrayStatus StatusFor(params PullRequest[] pullRequests)
    {
        var snapshot = Snapshot(pullRequests);
        return TrayStatusCalculator.Calculate(new GhResult.Success(snapshot), PrSections.From(snapshot));
    }

    [Fact]
    public void Failed_fetch_is_error()
    {
        Assert.Equal(TrayStatus.Error, TrayStatusCalculator.Calculate(new GhResult.NotAuthenticated(), null));
    }

    [Fact]
    public void Review_request_needs_attention()
    {
        Assert.Equal(TrayStatus.NeedsAttention, StatusFor(Create(PrGroups.ReviewRequested)));
    }

    [Fact]
    public void Changes_requested_on_my_pr_needs_attention_even_if_another_is_approved()
    {
        var approved = Create(PrGroups.Mine) with { Id = "A", AuthorLogin = Me, ReviewDecision = ReviewDecision.Approved };
        var rejected = Create(PrGroups.Mine) with { Id = "B", AuthorLogin = Me, ReviewDecision = ReviewDecision.ChangesRequested };

        Assert.Equal(TrayStatus.NeedsAttention, StatusFor(approved, rejected));
    }

    [Fact]
    public void Approved_pr_of_mine_is_approved()
    {
        Assert.Equal(TrayStatus.Approved, StatusFor(Create(PrGroups.Mine) with { AuthorLogin = Me, ReviewDecision = ReviewDecision.Approved }));
    }

    [Fact]
    public void Only_watched_prs_is_neutral()
    {
        Assert.Equal(TrayStatus.Neutral, StatusFor(Create(PrGroups.Watched)));
    }
}
