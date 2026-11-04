using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class NotificationSoundsTests
{
    private static readonly NotificationSoundSelection PullRequestSound = new(NotificationSound.Bell, null);
    private static readonly NotificationSoundSelection CommentSound = new(NotificationSound.Custom, "/home/me/kommentar.wav");
    private static readonly NotificationSounds Sounds = new(PullRequestSound, CommentSound);

    private static NotificationMessage Message(NotificationKind kind) => new("Tittel", "Tekst", "https://github.com/pulls", IsUrgent: false, kind);

    [Fact]
    public void Each_kind_sounds_once_per_batch_with_its_own_sound()
    {
        var sounds = Sounds.ForBatch([
            Message(NotificationKind.PullRequest),
            Message(NotificationKind.Comment),
            Message(NotificationKind.PullRequest),
            Message(NotificationKind.Comment),
        ]);

        Assert.Equal(new[] { PullRequestSound, CommentSound, NotificationSoundSelection.Silent, NotificationSoundSelection.Silent }, sounds);
    }

    [Fact]
    public void Batch_with_only_comments_uses_the_comment_sound()
    {
        var sounds = Sounds.ForBatch([Message(NotificationKind.Comment), Message(NotificationKind.Comment)]);

        Assert.Equal(new[] { CommentSound, NotificationSoundSelection.Silent }, sounds);
    }

    [Fact]
    public void Messages_are_pull_request_notifications_unless_marked_otherwise()
    {
        Assert.Equal(PullRequestSound, Sounds.For(new NotificationMessage("Tittel", "Tekst", "https://github.com/pulls", IsUrgent: false).Kind));
    }
}
