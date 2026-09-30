using Avalonia;
using Avalonia.Controls;
using PrTray.App;

// Global\ because each autostart/setsid launch gets its own Unix session, and session-scoped names never collide.
using var singleInstance = new Mutex(initiallyOwned: true, $"Global\\PrTray.SingleInstance.{Environment.UserName}", out var isFirstInstance);
if (!isFirstInstance)
    return;

AppBuilder.Configure<TrayApp>()
    .UsePlatformDetect()
    // Separate popup windows lose clicks outside the parent window under XWayland; drawn in-window, dropdowns fit the window instead.
    .With(new X11PlatformOptions { OverlayPopups = true })
    .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
