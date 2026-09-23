using PrTray.Core.Presentation;
using Tmds.DBus.Protocol;

namespace PrTray.App.Notifications;

public sealed class LinuxNotifier : INotifier
{
    private const string NotificationsService = "org.freedesktop.Notifications";
    private const string NotificationsPath = "/org/freedesktop/Notifications";
    private const byte NormalUrgency = 1;
    private const byte CriticalUrgency = 2;

    private static readonly string[] Actions = ["default", "Åpne", "open", "Åpne"];

    private readonly PendingNotifications pendingNotifications = new();
    private readonly Lazy<Task<IDisposable>> actionSubscription;

    public LinuxNotifier() => actionSubscription = new Lazy<Task<IDisposable>>(SubscribeToActionsAsync);

    public void Show(NotificationMessage message) => _ = ShowAsync(message);

    public void Dispose()
    {
        if (actionSubscription.IsValueCreated && actionSubscription.Value.IsCompletedSuccessfully)
            actionSubscription.Value.Result.Dispose();
    }

    private async Task ShowAsync(NotificationMessage message)
    {
        try
        {
            await actionSubscription.Value;
            var notificationId = await DBusConnection.Session.CallMethodAsync(
                CreateNotifyMessage(message),
                (Message reply, object? _) => reply.GetBodyReader().ReadUInt32(),
                null);
            pendingNotifications.Add(notificationId, message.Url);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"PrTray: could not show notification: {exception.Message}");
        }
    }

    private async Task<IDisposable> SubscribeToActionsAsync() =>
        await DBusConnection.Session.WatchSignalAsync<(uint NotificationId, string ActionKey)>(
            null,
            NotificationsPath,
            NotificationsService,
            "ActionInvoked",
            (Message signal, object? _) =>
            {
                var bodyReader = signal.GetBodyReader();
                return (bodyReader.ReadUInt32(), bodyReader.ReadString());
            },
            (Notification<(uint NotificationId, string ActionKey)> notification) =>
            {
                var url = notification.HasValue
                    ? pendingNotifications.TakeUrlForAction(notification.Value.NotificationId, notification.Value.ActionKey)
                    : null;
                if (url is not null)
                    UrlOpener.Open(url);
            },
            ObserverFlags.None,
            false,
            null);

    private static MessageBuffer CreateNotifyMessage(NotificationMessage message)
    {
        using var writer = DBusConnection.Session.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NotificationsService,
            path: NotificationsPath,
            @interface: NotificationsService,
            member: "Notify",
            signature: "susssasa{sv}i");
        writer.WriteString("PrTray");
        writer.WriteUInt32(0);
        writer.WriteString("");
        writer.WriteString(message.Title);
        writer.WriteString(NotificationMarkup.Escape(message.Body));
        writer.WriteArray(Actions);
        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["urgency"] = VariantValue.Byte(message.IsUrgent ? CriticalUrgency : NormalUrgency),
        });
        writer.WriteInt32(-1);
        return writer.CreateMessage();
    }
}
