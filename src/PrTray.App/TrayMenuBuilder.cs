using Avalonia.Controls;
using PrTray.Core.GitHub;
using PrTray.Core.Models;
using PrTray.Core.Presentation;

namespace PrTray.App;

public static class TrayMenuBuilder
{
    private const int MaxItemsPerSection = 10;

    public static void Populate(NativeMenu menu, PrSections? sections, GhResult? latestResult, DateTimeOffset? lastSuccessAt, TrayMenuActions actions)
    {
        menu.Items.Clear();
        var problem = latestResult is null ? null : PrLabels.Problem(latestResult);
        if (problem is not null)
            AddDisabled(menu, PrLabels.EscapeAccessKeys(problem));
        if (sections is not null)
        {
            AddSection(menu, "Mine PR-er", sections.Mine, actions);
            AddSection(menu, "Til review", sections.ToReview, actions);
            AddSection(menu, "Overvåkede repoer", sections.Watched, actions);
        }
        menu.Items.Add(new NativeMenuItemSeparator());
        AddAction(menu, "Vis oversikt…", actions.ShowOverview);
        AddAction(menu, "Oppdater nå", actions.RefreshNow);
        AddDisabled(menu, LastUpdatedText(lastSuccessAt, problem is not null));
        AddAction(menu, "Avslutt", actions.Quit);
    }

    private static void AddSection(NativeMenu menu, string heading, IReadOnlyList<PullRequest> pullRequests, TrayMenuActions actions)
    {
        if (pullRequests.Count == 0)
            return;
        AddDisabled(menu, $"{heading} ({pullRequests.Count})");
        foreach (var pullRequest in pullRequests.Take(MaxItemsPerSection))
            AddAction(menu, PrLabels.MenuLabel(pullRequest), () => actions.OpenUrl(pullRequest.Url));
        if (pullRequests.Count > MaxItemsPerSection)
            AddDisabled(menu, $"… og {pullRequests.Count - MaxItemsPerSection} til (se oversikt)");
        menu.Items.Add(new NativeMenuItemSeparator());
    }

    private static string LastUpdatedText(DateTimeOffset? lastSuccessAt, bool latestFailed) => lastSuccessAt switch
    {
        null when latestFailed => "Aldri oppdatert",
        null => "Henter…",
        _ => $"Sist oppdatert {lastSuccessAt.Value.ToLocalTime():HH:mm}{(latestFailed ? " (feilet)" : "")}",
    };

    private static void AddAction(NativeMenu menu, string header, Action onClick)
    {
        var item = new NativeMenuItem(header);
        item.Click += (_, _) => onClick();
        menu.Items.Add(item);
    }

    private static void AddDisabled(NativeMenu menu, string header) =>
        menu.Items.Add(new NativeMenuItem(header) { IsEnabled = false });
}
