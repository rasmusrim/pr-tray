namespace PrTray.Core.Presentation;

public static class NotificationMarkup
{
    public static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
