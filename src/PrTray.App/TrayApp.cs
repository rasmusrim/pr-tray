using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using PrTray.App.Notifications;
using PrTray.App.Overview;
using PrTray.App.Settings;
using PrTray.Core.GitHub;
using PrTray.Core.Models;
using PrTray.Core.Polling;
using PrTray.Core.Presentation;
using PrTray.Core.Storage;

namespace PrTray.App;

public sealed class TrayApp : Application
{
    private readonly NativeMenu menu = new();
    private readonly INotifier notifier = NotifierFactory.Create();
    private readonly ConfigStore configStore = new(AppPaths.ConfigFile);
    private PrTrayConfig config = PrTrayConfig.Default;
    private readonly GhClient ghClient = new(new ProcessRunner(), TimeProvider.System);
    private Poller? poller;
    private TrayIcon? trayIcon;
    private OverviewWindow? overviewWindow;
    private SettingsWindow? settingsWindow;
    private PrSnapshot? lastSnapshot;
    private GhResult? latestResult;

    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        config = configStore.LoadOrCreate();
        poller = new Poller(ghClient, new SeenStore(AppPaths.SeenFile), config, TimeProvider.System);
        poller.Polled += outcome => Dispatcher.UIThread.Post(() => OnPolled(outcome));

        trayIcon = new TrayIcon { Icon = TrayIconFactory.Create(TrayStatus.Neutral), ToolTipText = "PrTray", Menu = menu, IsVisible = true };
        trayIcon.Clicked += (_, _) => ShowOverview();
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });

        Refresh();
        poller.Start();
        base.OnFrameworkInitializationCompleted();
    }

    private PrSections? CurrentSections => lastSnapshot is null ? null : PrSections.From(lastSnapshot);

    private TrayMenuActions MenuActions => new(UrlOpener.Open, ShowOverview, ShowSettings, () => _ = poller?.RefreshNowAsync(), Quit);

    private void OnPolled(PollOutcome outcome)
    {
        latestResult = outcome.Result;
        if (outcome.Result is GhResult.Success success)
            lastSnapshot = success.Snapshot;
        foreach (var message in NotificationTexts.ForBatch(outcome.Events))
            notifier.Show(message);
        Refresh();
    }

    private void Refresh()
    {
        var sections = CurrentSections;
        trayIcon!.Icon = TrayIconFactory.Create(latestResult is null ? TrayStatus.Neutral : TrayStatusCalculator.Calculate(latestResult, sections));
        trayIcon.ToolTipText = sections is null ? "PrTray" : $"PrTray – {sections.Mine.Count} mine, {sections.ToReview.Count} til review";
        TrayMenuBuilder.Populate(menu, sections, latestResult, lastSnapshot?.FetchedAt, MenuActions);
        if (overviewWindow?.IsVisible == true)
            overviewWindow.ShowSections(sections, lastSnapshot?.FetchedAt, TimeProvider.System.GetUtcNow());
    }

    private void ShowOverview()
    {
        overviewWindow ??= new OverviewWindow(UrlOpener.Open);
        overviewWindow.ShowSections(CurrentSections, lastSnapshot?.FetchedAt, TimeProvider.System.GetUtcNow());
        overviewWindow.Show();
        overviewWindow.Activate();
    }

    private void ShowSettings()
    {
        if (settingsWindow is null)
        {
            settingsWindow = new SettingsWindow(
                config.Repositories,
                SaveRepositories,
                repository => ghClient.CheckRepositoryAsync(config, repository, CancellationToken.None));
            settingsWindow.Closed += (_, _) => settingsWindow = null;
        }
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    private void SaveRepositories(IReadOnlyList<string> repositories)
    {
        config = config with { Repositories = repositories };
        configStore.Save(config);
        poller?.UpdateConfig(config);
        _ = poller?.RefreshNowAsync();
    }

    private void Quit()
    {
        poller?.Dispose();
        notifier.Dispose();
        (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }
}
