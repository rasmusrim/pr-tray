using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public sealed class NotificationSoundSelectionTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    public void Dispose() => temporaryDirectory.Dispose();

    [Fact]
    public void Custom_sound_with_existing_file_is_kept()
    {
        Directory.CreateDirectory(temporaryDirectory.DirectoryPath);
        var soundFile = temporaryDirectory.FilePath("pling.wav");
        File.WriteAllBytes(soundFile, [0]);

        Assert.Equal(NotificationSound.Custom, new NotificationSoundSelection(NotificationSound.Custom, soundFile).EffectiveSound);
    }

    [Fact]
    public void Custom_sound_with_missing_file_falls_back_to_message()
    {
        var selection = new NotificationSoundSelection(NotificationSound.Custom, temporaryDirectory.FilePath("borte.wav"));

        Assert.Equal(NotificationSound.Message, selection.EffectiveSound);
    }

    [Fact]
    public void Built_in_sounds_are_unchanged()
    {
        Assert.Equal(NotificationSound.Bell, new NotificationSoundSelection(NotificationSound.Bell, null).EffectiveSound);
    }
}
