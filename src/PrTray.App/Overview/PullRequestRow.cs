using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PrTray.App.Styling;
using PrTray.Core.Models;
using PrTray.Core.Presentation;

namespace PrTray.App.Overview;

public sealed class PullRequestRow : Button
{
    public PullRequestRow(PullRequest pullRequest, DateTimeOffset now, Action<string> openUrl)
    {
        var statusColor = StatusColors.For(pullRequest);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(12, 8);
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);
        Content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Children = { StatusDot(statusColor), Texts(pullRequest, now), StatusChip(pullRequest, statusColor) },
        };
        Click += (_, _) => openUrl(pullRequest.Url);
    }

    protected override Type StyleKeyOverride => typeof(Button);

    private static Ellipse StatusDot(Color statusColor) => new()
    {
        Width = 10,
        Height = 10,
        Fill = new SolidColorBrush(statusColor),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static StackPanel Texts(PullRequest pullRequest, DateTimeOffset now)
    {
        var age = RelativeAge.Format(now - (pullRequest.MergedAt ?? pullRequest.CreatedAt));
        var texts = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(12, 0),
            Children =
            {
                new TextBlock { Text = pullRequest.Title, FontSize = 14, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = $"{pullRequest.DisplayName} · {pullRequest.AuthorLogin} · {age}", FontSize = 12, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis },
            },
        };
        Grid.SetColumn(texts, 1);
        return texts;
    }

    private static Border StatusChip(PullRequest pullRequest, Color statusColor)
    {
        var chip = new Border
        {
            Background = StatusColors.Subtle(statusColor),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = PrLabels.StatusText(pullRequest),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(statusColor),
            },
        };
        Grid.SetColumn(chip, 2);
        return chip;
    }
}
