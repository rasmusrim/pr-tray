using PrTray.Core.GitHub;
using PrTray.Core.Models;
using PrTray.Core.Presentation;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Presentation;

public class PrLabelsTests
{
    [Theory]
    [InlineData(PrState.Merged, false, ReviewDecision.Approved, "🟣")]
    [InlineData(PrState.Open, true, ReviewDecision.Approved, "📝")]
    [InlineData(PrState.Open, false, ReviewDecision.ChangesRequested, "❌")]
    [InlineData(PrState.Open, false, ReviewDecision.Approved, "✅")]
    [InlineData(PrState.Open, false, ReviewDecision.ReviewRequired, "⏳")]
    public void Status_emoji_reflects_state(PrState state, bool isDraft, ReviewDecision decision, string expected)
    {
        var pullRequest = Create(PrGroups.Mine) with { State = state, IsDraft = isDraft, ReviewDecision = decision };

        Assert.Equal(expected, PrLabels.StatusEmoji(pullRequest));
    }

    [Fact]
    public void Menu_label_truncates_long_titles_and_doubles_underscores()
    {
        var pullRequest = Create(PrGroups.Mine) with { Title = "fix_login " + new string('x', 80) };

        var label = PrLabels.MenuLabel(pullRequest);

        Assert.StartsWith("⏳ acme/widgets#70  fix__login ", label);
        Assert.EndsWith("…", label);
    }

    [Fact]
    public void Problem_texts_describe_failures()
    {
        Assert.Equal("gh ikke funnet – installer GitHub CLI", PrLabels.Problem(new GhResult.NotInstalled()));
        Assert.Equal("gh ikke innlogget – kjør gh auth login", PrLabels.Problem(new GhResult.NotAuthenticated()));
        Assert.Equal("Henting feilet: HTTP 502", PrLabels.Problem(new GhResult.Failed("HTTP 502")));
        Assert.Null(PrLabels.Problem(new GhResult.Success(Snapshot())));
    }
}
