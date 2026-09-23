using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PrTray.Core.Models;
using PrTray.Core.Presentation;

namespace PrTray.App;

public sealed class OverviewWindow : Window
{
    private readonly StackPanel rows = new() { Spacing = 2, Margin = new Thickness(16) };
    private readonly Action<string> openUrl;

    public OverviewWindow(Action<string> openUrl)
    {
        this.openUrl = openUrl;
        Title = "PrTray – oversikt";
        Width = 760;
        Height = 560;
        Content = new ScrollViewer { Content = rows };
        Closing += (_, closingArguments) =>
        {
            if (closingArguments.CloseReason == WindowCloseReason.ApplicationShutdown)
                return;
            closingArguments.Cancel = true;
            Hide();
        };
    }

    public void ShowSections(PrSections? sections, DateTimeOffset now)
    {
        rows.Children.Clear();
        if (sections is null)
        {
            rows.Children.Add(new TextBlock { Text = "Henter PR-er…" });
            return;
        }
        AddSection("Mine PR-er", sections.Mine, now);
        AddSection("Til review", sections.ToReview, now);
        AddSection("Overvåkede repoer", sections.Watched, now);
        AddSection("Nylig merget", sections.RecentlyMerged, now);
    }

    private void AddSection(string heading, IReadOnlyList<PullRequest> pullRequests, DateTimeOffset now)
    {
        rows.Children.Add(new TextBlock
        {
            Text = $"{heading} ({pullRequests.Count})",
            FontWeight = FontWeight.Bold,
            FontSize = 16,
            Margin = new Thickness(0, 12, 0, 4),
        });
        if (pullRequests.Count == 0)
        {
            rows.Children.Add(new TextBlock { Text = "Ingen", Opacity = 0.6 });
            return;
        }
        foreach (var pullRequest in pullRequests)
            rows.Children.Add(CreateRow(pullRequest, now));
    }

    private Button CreateRow(PullRequest pullRequest, DateTimeOffset now)
    {
        var age = RelativeAge.Format(now - (pullRequest.MergedAt ?? pullRequest.CreatedAt));
        var row = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Background = Brushes.Transparent,
            Content = new TextBlock
            {
                Text = $"{PrLabels.StatusEmoji(pullRequest)}  {pullRequest.DisplayName}  {pullRequest.Title}  · {pullRequest.AuthorLogin} · {age}",
                TextTrimming = TextTrimming.CharacterEllipsis,
            },
        };
        row.Click += (_, _) => openUrl(pullRequest.Url);
        return row;
    }
}
