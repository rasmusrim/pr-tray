using PrTray.Core.Detection;
using PrTray.Core.Models;
using PrTray.Core.Presentation;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Presentation;

public class NotificationTextsTests
{
    private static readonly PullRequest PullRequest = Create(PrGroups.Mine);

    [Theory]
    [InlineData(PrEventKind.Opened, "🆕 Ny PR fra colleague", false)]
    [InlineData(PrEventKind.NewCommitsSinceUnapprovedReview, "🔁 Nye commits etter review uten godkjenning", true)]
    [InlineData(PrEventKind.Approved, "✅ colleague godkjente PR-en", false)]
    [InlineData(PrEventKind.ChangesRequested, "❌ colleague ba om endringer", true)]
    [InlineData(PrEventKind.ReviewRequested, "👀 Du er bedt om review", true)]
    [InlineData(PrEventKind.ReadyForReview, "📣 colleague publiserte utkastet", false)]
    [InlineData(PrEventKind.Merged, "🟣 PR merget", false)]
    public void Message_per_event_kind(PrEventKind kind, string expectedTitle, bool expectedUrgent)
    {
        var message = NotificationTexts.For(new PrEvent(kind, PullRequest, "colleague"));

        Assert.Equal(expectedTitle, message.Title);
        Assert.Equal("acme/widgets#70: Legg til historikk", message.Body);
        Assert.Equal(PullRequest.Url, message.Url);
        Assert.Equal(expectedUrgent, message.IsUrgent);
    }

    [Fact]
    public void Batch_above_limit_is_capped_with_summary()
    {
        var events = Enumerable.Range(0, 9).Select(_ => new PrEvent(PrEventKind.Merged, PullRequest, null)).ToList();

        var messages = NotificationTexts.ForBatch(events);

        Assert.Equal(NotificationTexts.MaxIndividualNotifications, messages.Count);
        Assert.Equal("… og 5 hendelser til", messages[^1].Title);
    }

    [Fact]
    public void Batch_within_limit_is_unchanged()
    {
        var events = Enumerable.Range(0, 5).Select(_ => new PrEvent(PrEventKind.Merged, PullRequest, null)).ToList();

        Assert.All(NotificationTexts.ForBatch(events), message => Assert.Equal("🟣 PR merget", message.Title));
    }

    [Fact]
    public void Markup_escape_handles_ampersand_and_angle_brackets()
    {
        Assert.Equal("Ny SMS-flyt &amp; &lt;varsler&gt;", NotificationMarkup.Escape("Ny SMS-flyt & <varsler>"));
    }
}
