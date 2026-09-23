using Avalonia;
using Avalonia.Controls;
using PrTray.App;

using var singleInstance = new Mutex(initiallyOwned: true, "PrTray.SingleInstance", out var isFirstInstance);
if (!isFirstInstance)
    return;

AppBuilder.Configure<TrayApp>()
    .UsePlatformDetect()
    .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
