using Avalonia;
using Avalonia.Controls;
using PrTray.App;

// Global\ because each autostart/setsid launch gets its own Unix session, and session-scoped names never collide.
using var singleInstance = new Mutex(initiallyOwned: true, $"Global\\PrTray.SingleInstance.{Environment.UserName}", out var isFirstInstance);
if (!isFirstInstance)
    return;

AppBuilder.Configure<TrayApp>()
    .UsePlatformDetect()
    .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
