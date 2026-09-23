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
        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($env:PRTRAY_APP_ID).Show([Windows.UI.Notifications.ToastNotification]::new($template))
        """;

    public void Show(NotificationMessage message)
    {
        var startInfo = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", ToastScript })
            startInfo.ArgumentList.Add(argument);
        startInfo.Environment["PRTRAY_TITLE"] = message.Title;
        startInfo.Environment["PRTRAY_BODY"] = message.Body;
        startInfo.Environment["PRTRAY_APP_ID"] = PowerShellAppId;
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
}
