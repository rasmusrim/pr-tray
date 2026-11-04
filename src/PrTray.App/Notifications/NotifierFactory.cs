namespace PrTray.App.Notifications;

public static class NotifierFactory
{
    public static INotifier Create(Action<string> openUrl) =>
        OperatingSystem.IsWindows() ? new WindowsNotifier()
        : OperatingSystem.IsMacOS() ? new MacNotifier()
        : new LinuxNotifier(openUrl);
}
