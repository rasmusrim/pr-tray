namespace PrTray.Core.Presentation;

public sealed record NotificationMessage(string Title, string Body, string Url, bool IsUrgent);
