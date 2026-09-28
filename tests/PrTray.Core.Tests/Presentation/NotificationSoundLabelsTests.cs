using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class NotificationSoundLabelsTests
{
    [Fact]
    public void Every_sound_has_a_label()
    {
        foreach (var sound in Enum.GetValues<NotificationSound>())
            Assert.False(string.IsNullOrWhiteSpace(NotificationSoundLabels.For(sound)));
    }

    [Fact]
    public void Labels_are_norwegian()
    {
        Assert.Equal("Ingen lyd", NotificationSoundLabels.For(NotificationSound.None));
        Assert.Equal("Egen lydfil…", NotificationSoundLabels.For(NotificationSound.Custom));
    }
}
