using System.ComponentModel;
using System.Diagnostics;
using PrTray.Core.Presentation;

namespace PrTray.App.Notifications;

public sealed class WindowsNotifier : INotifier
{
    private const string PowerShellAppId = @"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}\WindowsPowerShell\v1.0\powershell.exe";

    private const string ToastScript = """
        [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
        $template = [Windows.UI.Notifications.ToastNotificationManager]::GetTemplateContent([Windows.UI.Notifications.ToastTemplateType]::ToastText02)
        $texts = $template.GetElementsByTagName('text')
        $texts.Item(0).AppendChild($template.CreateTextNode($env:PRTRAY_TITLE)) | Out-Null
        $texts.Item(1).AppendChild($template.CreateTextNode($env:PRTRAY_BODY)) | Out-Null
        $audio = $template.CreateElement('audio')
        if ($env:PRTRAY_SOUND) { $audio.SetAttribute('src', $env:PRTRAY_SOUND) } else { $audio.SetAttribute('silent', 'true') }
        $template.DocumentElement.AppendChild($audio) | Out-Null
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($env:PRTRAY_APP_ID).Show([Windows.UI.Notifications.ToastNotification]::new($template))
        if ($env:PRTRAY_SOUND_FILE) { (New-Object System.Media.SoundPlayer $env:PRTRAY_SOUND_FILE).PlaySync() }
        """;

    public void Show(NotificationMessage message, NotificationSoundSelection sound)
    {
        var startInfo = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", ToastScript })
            startInfo.ArgumentList.Add(argument);
        startInfo.Environment["PRTRAY_TITLE"] = message.Title;
        startInfo.Environment["PRTRAY_BODY"] = message.Body;
        startInfo.Environment["PRTRAY_APP_ID"] = PowerShellAppId;
        startInfo.Environment["PRTRAY_SOUND"] = SoundEventUri(sound.EffectiveSound);
        startInfo.Environment["PRTRAY_SOUND_FILE"] = sound.EffectiveSound == NotificationSound.Custom ? sound.CustomSoundFile : "";
        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception exception)
        {
            Console.Error.WriteLine($"PrTray: toast failed: {exception.Message}");
        }
    }

    public void Dispose()
    {
    }

    private static string SoundEventUri(NotificationSound sound) => sound switch
    {
        NotificationSound.Message => "ms-winsoundevent:Notification.IM",
        NotificationSound.Email => "ms-winsoundevent:Notification.Mail",
        NotificationSound.Complete => "ms-winsoundevent:Notification.Default",
        NotificationSound.Bell => "ms-winsoundevent:Notification.SMS",
        NotificationSound.Reminder => "ms-winsoundevent:Notification.Reminder",
        _ => "",
    };
}
