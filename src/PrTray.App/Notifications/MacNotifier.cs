using System.ComponentModel;
using System.Diagnostics;
using PrTray.Core.Presentation;

namespace PrTray.App.Notifications;

public sealed class MacNotifier : INotifier
{
    public void Show(NotificationMessage message)
    {
        var startInfo = new ProcessStartInfo("osascript") { UseShellExecute = false };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add($"display notification {AppleScriptString(message.Body)} with title {AppleScriptString(message.Title)}");
        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception exception)
        {
            Console.Error.WriteLine($"PrTray: osascript failed: {exception.Message}");
        }
    }

    private static string AppleScriptString(string text) =>
        "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public void Dispose()
    {
    }
}
