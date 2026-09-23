namespace PrTray.App;

public sealed record TrayMenuActions(Action<string> OpenUrl, Action ShowOverview, Action RefreshNow, Action Quit);
