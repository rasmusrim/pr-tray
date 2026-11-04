using PrTray.Core.Detection;
using PrTray.Core.Models;

namespace PrTray.Core.Presentation;

public static class NotificationTexts
{
    public const int MaxIndividualNotifications = 5;

    public const int MaxCommentPreviewLength = 120;

    private const string PullRequestsOverviewUrl = "https://github.com/pulls";

    public static NotificationMessage SoundPreview { get; } =
        new("🔔 Slik høres varslene fra PrTray ut", "Lagre innstillingene for å bruke denne lyden.", PullRequestsOverviewUrl, IsUrgent: false);

    public static IReadOnlyList<NotificationMessage> ForBatch(IReadOnlyList<PrEvent> events)
    {
        if (events.Count <= MaxIndividualNotifications)
            return events.Select(For).ToList();
        var messages = events.Take(MaxIndividualNotifications - 1).Select(For).ToList();
        var remainingCount = events.Count - messages.Count;
        messages.Add(new NotificationMessage($"… og {remainingCount} hendelser til", "Åpne PrTray-oversikten for detaljer.", PullRequestsOverviewUrl, IsUrgent: false));
        return messages;
    }

    public static NotificationMessage For(PrEvent prEvent)
    {
        var pullRequest = prEvent.PullRequest;
        var body = $"{pullRequest.DisplayName}: {pullRequest.Title}";
        return prEvent.Kind switch
        {
            PrEventKind.Opened => new($"🆕 Ny PR fra {prEvent.ActorLogin}", body, pullRequest.Url, IsUrgent: false),
            PrEventKind.NewCommitsSinceUnapprovedReview => new("🔁 Nye commits etter review uten godkjenning", body, pullRequest.Url, IsUrgent: true),
            PrEventKind.Approved => new($"✅ {prEvent.ActorLogin} godkjente PR-en", body, pullRequest.Url, IsUrgent: false),
            PrEventKind.ChangesRequested => new($"❌ {prEvent.ActorLogin} ba om endringer", body, pullRequest.Url, IsUrgent: true),
            PrEventKind.ReviewRequested => new("👀 Du er bedt om review", body, pullRequest.Url, IsUrgent: true),
            PrEventKind.ReadyForReview => new($"📣 {prEvent.ActorLogin} publiserte utkastet", body, pullRequest.Url, IsUrgent: false),
            PrEventKind.Commented => ForComments(prEvent.Comments, body),
            PrEventKind.Merged => new("🟣 PR merget", body, pullRequest.Url, IsUrgent: false),
            _ => throw new ArgumentOutOfRangeException(nameof(prEvent), prEvent.Kind, null),
        };
    }

    private static NotificationMessage ForComments(IReadOnlyList<Comment> comments, string pullRequestLine)
    {
        var authors = comments.Select(comment => comment.AuthorLogin).Distinct().ToList();
        var title = comments.Count == 1
            ? $"💬 {authors[0]} kommenterte"
            : $"💬 {comments.Count} nye kommentarer fra {JoinNames(authors)}";
        var previewLine = PreviewLine(comments, namesAuthor: authors.Count > 1);
        var body = previewLine is null ? pullRequestLine : $"{pullRequestLine}\n{previewLine}";
        return new NotificationMessage(title, body, comments[0].Url, IsUrgent: true, NotificationKind.Comment);
    }

    // Inline replies in review threads have no text of their own here, so preview the first comment that has some.
    private static string? PreviewLine(IReadOnlyList<Comment> comments, bool namesAuthor)
    {
        var firstWithText = comments.FirstOrDefault(comment => !string.IsNullOrWhiteSpace(comment.BodyText));
        if (firstWithText is null)
            return null;
        var singleLineText = string.Join(' ', firstWithText.BodyText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var quote = $"«{PrLabels.Truncate(singleLineText, MaxCommentPreviewLength)}»";
        return namesAuthor ? $"{firstWithText.AuthorLogin}: {quote}" : quote;
    }

    private static string JoinNames(IReadOnlyList<string> names) =>
        names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} og {names[^1]}";
}
