using System.ComponentModel;
using System.Diagnostics;
using PrTray.Core.Presentation;

namespace PrTray.App.Notifications;

public sealed class MacNotifier : INotifier
{
    public void Show(NotificationMessage message, NotificationSoundSelection sound)
    {
        var soundClause = SystemSoundName(sound.EffectiveSound) is { } soundName ? $" sound name {AppleScriptString(soundName)}" : "";
        Run("osascript", "-e", $"display notification {AppleScriptString(message.Body)} with title {AppleScriptString(message.Title)}{soundClause}");
        if (sound.EffectiveSound == NotificationSound.Custom)
            Run("afplay", sound.CustomSoundFile!);
    }

    public void Dispose()
    {
    }

    private static string? SystemSoundName(NotificationSound sound) => sound switch
    {
        NotificationSound.Message => "Glass",
        NotificationSound.Email => "Ping",
        NotificationSound.Complete => "Hero",
        NotificationSound.Bell => "Tink",
        NotificationSound.Reminder => "Funk",
        _ => null,
    };

    private static void Run(string fileName, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Win32Exception exception)
        {
            Console.Error.WriteLine($"PrTray: {fileName} failed: {exception.Message}");
        }
    }

    private static string AppleScriptString(string text) =>
        "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
