using PrTray.Core.Presentation;

namespace PrTray.Core.Tests.Presentation;

public class PendingNotificationsTests
{
    private const string Url = "https://github.com/acme/widgets/pull/72";

    [Theory]
    [InlineData("default")]
    [InlineData("open")]
    public void Open_actions_return_the_url_once(string actionKey)
    {
        var pendingNotifications = new PendingNotifications();
        pendingNotifications.Add(41, Url);

        Assert.Equal(Url, pendingNotifications.TakeUrlForAction(41, actionKey));
        Assert.Null(pendingNotifications.TakeUrlForAction(41, actionKey));
    }

    [Fact]
    public void Other_actions_and_unknown_notifications_return_nothing()
    {
        var pendingNotifications = new PendingNotifications();
        pendingNotifications.Add(41, Url);

        Assert.Null(pendingNotifications.TakeUrlForAction(41, "dismiss"));
        Assert.Null(pendingNotifications.TakeUrlForAction(99, "default"));
        Assert.Equal(Url, pendingNotifications.TakeUrlForAction(41, "default"));
    }

    [Fact]
    public void Oldest_notifications_are_forgotten_beyond_the_capacity()
    {
        var pendingNotifications = new PendingNotifications();
        for (uint notificationId = 1; notificationId <= PendingNotifications.Capacity + 1; notificationId++)
            pendingNotifications.Add(notificationId, $"{Url}?{notificationId}");

        Assert.Null(pendingNotifications.TakeUrlForAction(1, "default"));
        Assert.Equal($"{Url}?2", pendingNotifications.TakeUrlForAction(2, "default"));
    }
}
