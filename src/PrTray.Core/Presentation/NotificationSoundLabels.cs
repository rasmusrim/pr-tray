namespace PrTray.Core.Presentation;

public static class NotificationSoundLabels
{
    public static string For(NotificationSound sound) => sound switch
    {
        NotificationSound.None => "Ingen lyd",
        NotificationSound.Message => "Melding",
        NotificationSound.Email => "E-post",
        NotificationSound.Complete => "Fullført",
        NotificationSound.Bell => "Bjelle",
        NotificationSound.Reminder => "Påminnelse",
        NotificationSound.Custom => "Egen lydfil…",
        _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, null),
    };
}
