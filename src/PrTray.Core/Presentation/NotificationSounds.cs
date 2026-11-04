namespace PrTray.Core.Presentation;

public sealed record NotificationSounds(NotificationSoundSelection PullRequests, NotificationSoundSelection Comments)
{
    public NotificationSoundSelection For(NotificationKind kind) => kind == NotificationKind.Comment ? Comments : PullRequests;

    // Each kind sounds once per batch, so several notifications at once do not become several sounds of the same kind.
    public IReadOnlyList<NotificationSoundSelection> ForBatch(IReadOnlyList<NotificationMessage> messages)
    {
        var kindsAlreadySounded = new HashSet<NotificationKind>();
        return messages
            .Select(message => kindsAlreadySounded.Add(message.Kind) ? For(message.Kind) : NotificationSoundSelection.Silent)
            .ToList();
    }
}
