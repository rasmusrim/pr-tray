using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PrTray.App.Styling;
using PrTray.Core.Models;

namespace PrTray.App.Overview;

public sealed class SectionCard : Border
{
    public SectionCard(string heading, IReadOnlyList<PullRequest> pullRequests, DateTimeOffset now, Action<string> openUrl)
    {
        Background = StatusColors.CardBackground;
        CornerRadius = new CornerRadius(8);
        Padding = new Thickness(4, 8);
        Margin = new Thickness(0, 0, 0, 12);

        var content = new StackPanel { Spacing = 2 };
        content.Children.Add(Header(heading, pullRequests.Count));
        if (pullRequests.Count == 0)
            content.Children.Add(new TextBlock { Text = "Ingen", Opacity = 0.5, Margin = new Thickness(12, 4, 12, 8) });
        foreach (var pullRequest in pullRequests)
            content.Children.Add(new PullRequestRow(pullRequest, now, openUrl));
        Child = content;
    }

    protected override Type StyleKeyOverride => typeof(Border);

    private static StackPanel Header(string heading, int count) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 8,
        Margin = new Thickness(12, 4, 12, 6),
        Children =
        {
            new TextBlock { Text = heading, FontSize = 15, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
            new Border
            {
                Background = StatusColors.CardBackground,
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(7, 1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = count.ToString(), FontSize = 12, Opacity = 0.8 },
            },
        },
    };
}
