namespace PrTray.Core.Presentation;

public sealed record NotificationSoundSelection(NotificationSound Sound, string? CustomSoundFile)
{
    public static NotificationSoundSelection Silent { get; } = new(NotificationSound.None, null);

    public NotificationSound EffectiveSound =>
        Sound == NotificationSound.Custom && !File.Exists(CustomSoundFile) ? NotificationSound.Message : Sound;
}
