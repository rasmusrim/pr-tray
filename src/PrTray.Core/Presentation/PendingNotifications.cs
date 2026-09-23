namespace PrTray.Core.Presentation;

public sealed class PendingNotifications
{
    public const int Capacity = 100;

    private static readonly HashSet<string> OpenActionKeys = ["default", "open"];

    private readonly Lock gate = new();
    private readonly Dictionary<uint, string> urlByNotificationId = [];
    private readonly Queue<uint> insertionOrder = new();

    public void Add(uint notificationId, string url)
    {
        lock (gate)
        {
            urlByNotificationId[notificationId] = url;
            insertionOrder.Enqueue(notificationId);
            while (insertionOrder.Count > Capacity)
                urlByNotificationId.Remove(insertionOrder.Dequeue());
        }
    }

    public string? TakeUrlForAction(uint notificationId, string actionKey)
    {
        if (!OpenActionKeys.Contains(actionKey))
            return null;
        lock (gate)
            return urlByNotificationId.Remove(notificationId, out var url) ? url : null;
    }
}
