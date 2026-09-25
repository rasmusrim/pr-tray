using PrTray.Core.Detection;

namespace PrTray.Core.Presentation;

public static class NotificationTexts
{
    public const int MaxIndividualNotifications = 5;

    private const string PullRequestsOverviewUrl = "https://github.com/pulls";

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
            PrEventKind.Merged => new("🟣 PR merget", body, pullRequest.Url, IsUrgent: false),
            _ => throw new ArgumentOutOfRangeException(nameof(prEvent), prEvent.Kind, null),
        };
    }
}
