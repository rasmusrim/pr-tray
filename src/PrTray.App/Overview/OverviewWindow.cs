using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using PrTray.Core.Presentation;

namespace PrTray.App.Overview;

public sealed class OverviewWindow : Window
{
    private readonly StackPanel sectionCards = new();
    private readonly TextBlock lastUpdated = new() { FontSize = 12, Opacity = 0.65 };
    private readonly Action<string> openUrl;

    public OverviewWindow(Action<string> openUrl)
    {
        this.openUrl = openUrl;
        Title = "PrTray – oversikt";
        Width = 780;
        Height = 680;
        MinWidth = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var header = new StackPanel
        {
            Spacing = 2,
            Margin = new Thickness(0, 0, 0, 16),
            Children = { new TextBlock { Text = "Pull requests", FontSize = 22, FontWeight = FontWeight.SemiBold }, lastUpdated },
        };
        DockPanel.SetDock(header, Dock.Top);
        Content = new DockPanel
        {
            Margin = new Thickness(20, 16, 20, 0),
            Children = { header, new ScrollViewer { Content = sectionCards, Padding = new Thickness(0, 0, 8, 16) } },
        };
        Closing += (_, closingArguments) =>
        {
            if (closingArguments.CloseReason == WindowCloseReason.ApplicationShutdown)
                return;
            closingArguments.Cancel = true;
            Hide();
        };
    }

    public void ShowSections(PrSections? sections, DateTimeOffset? fetchedAt, DateTimeOffset now)
    {
        lastUpdated.Text = fetchedAt is null ? "Henter PR-er…" : $"Sist oppdatert {fetchedAt.Value.ToLocalTime():HH:mm}";
        sectionCards.Children.Clear();
        if (sections is null)
            return;
        sectionCards.Children.Add(new SectionCard("Mine PR-er", sections.Mine, now, openUrl));
        sectionCards.Children.Add(new SectionCard("Til review", sections.ToReview, now, openUrl));
        sectionCards.Children.Add(new SectionCard("Overvåkede repoer", sections.Watched, now, openUrl));
        sectionCards.Children.Add(new SectionCard("Nylig merget", sections.RecentlyMerged, now, openUrl));
    }
}
