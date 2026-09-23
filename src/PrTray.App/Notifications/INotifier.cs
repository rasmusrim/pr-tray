using PrTray.Core.Presentation;

namespace PrTray.App.Notifications;

public interface INotifier : IDisposable
{
    void Show(NotificationMessage message);
}
