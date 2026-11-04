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
        Assert.Equal(NotificationKind.PullRequest, message.Kind);
    }

    private static PrEvent CommentedEvent(params Comment[] comments) =>
        new(PrEventKind.Commented, PullRequest, comments[0].AuthorLogin) { Comments = comments };

    [Fact]
    public void Single_comment_names_the_author_previews_the_text_and_opens_the_comment()
    {
        var message = NotificationTexts.For(CommentedEvent(CommentBy("colleague")));

        Assert.Equal("💬 colleague kommenterte", message.Title);
        Assert.Equal("acme/widgets#70: Legg til historikk\n«Kan du se på navngivningen her?»", message.Body);
        Assert.Equal("https://github.com/acme/widgets/pull/70#issuecomment-C_1", message.Url);
        Assert.True(message.IsUrgent);
        Assert.Equal(NotificationKind.Comment, message.Kind);
    }

    [Fact]
    public void Several_comments_are_counted_and_open_the_first_one()
    {
        var message = NotificationTexts.For(CommentedEvent(
            CommentBy("colleague", id: "C_1", bodyText: ""),
            CommentBy("teammate", id: "C_2", bodyText: "Enig!"),
            CommentBy("reviewer", id: "C_3")));

        Assert.Equal("💬 3 nye kommentarer fra colleague, teammate og reviewer", message.Title);
        Assert.Equal("acme/widgets#70: Legg til historikk\nteammate: «Enig!»", message.Body);
        Assert.Equal("https://github.com/acme/widgets/pull/70#issuecomment-C_1", message.Url);
    }

    [Fact]
    public void Several_comments_from_one_person_name_them_once()
    {
        var message = NotificationTexts.For(CommentedEvent(CommentBy("colleague", id: "C_1"), CommentBy("colleague", id: "C_2")));

        Assert.Equal("💬 2 nye kommentarer fra colleague", message.Title);
        Assert.Equal("acme/widgets#70: Legg til historikk\n«Kan du se på navngivningen her?»", message.Body);
    }

    [Fact]
    public void Comment_without_text_shows_only_the_pull_request()
    {
        var message = NotificationTexts.For(CommentedEvent(CommentBy("colleague", bodyText: "")));

        Assert.Equal("acme/widgets#70: Legg til historikk", message.Body);
    }

    [Fact]
    public void Long_multiline_comment_is_previewed_on_one_shortened_line()
    {
        var message = NotificationTexts.For(CommentedEvent(CommentBy("colleague", bodyText: "Første linje\n\n  " + new string('x', 200))));

        var previewLine = message.Body.Split('\n')[1];
        Assert.StartsWith("«Første linje xxx", previewLine);
        Assert.EndsWith("…»", previewLine);
        Assert.Equal(NotificationTexts.MaxCommentPreviewLength + "«»".Length, previewLine.Length);
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
    public void Sound_preview_is_not_urgent()
    {
        Assert.Equal("🔔 Slik høres varslene fra PrTray ut", NotificationTexts.SoundPreview.Title);
        Assert.False(NotificationTexts.SoundPreview.IsUrgent);
    }

    [Fact]
    public void Markup_escape_handles_ampersand_and_angle_brackets()
    {
        Assert.Equal("Ny SMS-flyt &amp; &lt;varsler&gt;", NotificationMarkup.Escape("Ny SMS-flyt & <varsler>"));
    }
}
