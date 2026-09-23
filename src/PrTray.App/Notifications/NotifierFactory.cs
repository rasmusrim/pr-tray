namespace PrTray.App.Notifications;

public static class NotifierFactory
{
    public static INotifier Create() =>
        OperatingSystem.IsWindows() ? new WindowsNotifier()
        : OperatingSystem.IsMacOS() ? new MacNotifier()
        : new LinuxNotifier();
}
