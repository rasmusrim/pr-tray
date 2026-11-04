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
    public PullRequestRow(PullRequest pullRequest, DateTimeOffset now, UnreadComment? unreadComment, Action<string> openUrl)
    {
        var statusColor = StatusColors.For(pullRequest);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(12, 8);
        Background = Brushes.Transparent;
        Cursor = new Cursor(StandardCursorType.Hand);
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            Children = { StatusDot(statusColor), Texts(pullRequest, now), Chip(PrLabels.StatusText(pullRequest), statusColor, column: 3) },
        };
        if (unreadComment is not null)
            grid.Children.Add(Chip($"{PrLabels.UnreadCommentsMarker} Nye kommentarer", StatusColors.UnreadComments, column: 2, new Thickness(0, 0, 6, 0)));
        Content = grid;
        Click += (_, _) => openUrl(unreadComment?.FirstCommentUrl ?? pullRequest.Url);
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
                new TextBlock { Text = pullRequest.Title, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis },
                new TextBlock { Text = $"{pullRequest.DisplayName} · {pullRequest.AuthorLogin} · {age}", Classes = { TextStyles.Caption }, Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis },
            },
        };
        Grid.SetColumn(texts, 1);
        return texts;
    }

    private static Border Chip(string text, Color color, int column, Thickness margin = default)
    {
        var chip = new Border
        {
            Background = StatusColors.Subtle(color),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2),
            Margin = margin,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = text,
                Classes = { TextStyles.Caption },
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(color),
            },
        };
        Grid.SetColumn(chip, column);
        return chip;
    }
}
