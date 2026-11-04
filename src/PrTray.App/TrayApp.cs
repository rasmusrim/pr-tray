using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using PrTray.App.Notifications;
using PrTray.App.Overview;
using PrTray.App.Settings;
using PrTray.App.Styling;
using PrTray.Core.GitHub;
using PrTray.Core.Models;
using PrTray.Core.Polling;
using PrTray.Core.Presentation;
using PrTray.Core.Storage;

namespace PrTray.App;

public sealed class TrayApp : Application
{
    private readonly NativeMenu menu = new();
    private readonly TextStyles textStyles = new();
    private readonly INotifier notifier;
    private readonly ConfigStore configStore = new(AppPaths.ConfigFile);
    private readonly UnreadCommentsStore unreadCommentsStore = new(AppPaths.UnreadCommentsFile);
    private PrTrayConfig config = PrTrayConfig.Default;
    private readonly GhClient ghClient = new(new ProcessRunner(), TimeProvider.System);
    private Poller? poller;
    private TrayIcon? trayIcon;
    private OverviewWindow? overviewWindow;
    private SettingsWindow? settingsWindow;
    private PrSnapshot? lastSnapshot;
    private GhResult? latestResult;
    private UnreadComments unreadComments = UnreadComments.None;

    // Notification clicks arrive on a D-Bus thread.
    public TrayApp() => notifier = NotifierFactory.Create(url => Dispatcher.UIThread.Post(() => OpenUrl(url)));

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(textStyles);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        config = configStore.LoadOrCreate();
        unreadComments = unreadCommentsStore.Load();
        textStyles.Apply(Resources, config.TextSize);
        poller = new Poller(ghClient, new SeenStore(AppPaths.SeenFile), config, TimeProvider.System);
        poller.Polled += outcome => Dispatcher.UIThread.Post(() => OnPolled(outcome));

        trayIcon = new TrayIcon { Icon = TrayIconFactory.Create(TrayStatus.Neutral, unreadComments.Count > 0), ToolTipText = "PrTray", Menu = menu, IsVisible = true };
        trayIcon.Clicked += (_, _) => ShowOverview();
        TrayIcon.SetIcons(this, new TrayIcons { trayIcon });

        Refresh();
        poller.Start();
        base.OnFrameworkInitializationCompleted();
    }

    private NotificationSounds ConfiguredSounds => new(
        new NotificationSoundSelection(config.NotificationSound, config.CustomSoundFile),
        new NotificationSoundSelection(config.CommentNotificationSound, config.CommentCustomSoundFile));

    private PrSections? CurrentSections => lastSnapshot is null ? null : PrSections.From(lastSnapshot);

    private TrayMenuActions MenuActions => new(OpenUrl, ShowOverview, ShowSettings, () => _ = poller?.RefreshNowAsync(), Quit);

    private void OnPolled(PollOutcome outcome)
    {
        latestResult = outcome.Result;
        if (outcome.Result is GhResult.Success success)
        {
            lastSnapshot = success.Snapshot;
            UpdateUnreadComments(unreadComments.Add(outcome.Events).KeepRelevant(success.Snapshot));
        }
        var messages = NotificationTexts.ForBatch(outcome.Events);
        var sounds = ConfiguredSounds.ForBatch(messages);
        for (var index = 0; index < messages.Count; index++)
            notifier.Show(messages[index], sounds[index]);
        Refresh();
    }

    private void OpenUrl(string url)
    {
        UrlOpener.Open(url);
        UpdateUnreadComments(unreadComments.MarkOpened(url));
        // The click may come from a menu item that Refresh replaces, so rebuild after the click has been handled.
        Dispatcher.UIThread.Post(Refresh);
    }

    private void UpdateUnreadComments(UnreadComments updated)
    {
        if (updated.Entries.SequenceEqual(unreadComments.Entries))
            return;
        unreadComments = updated;
        unreadCommentsStore.Save(unreadComments);
    }

    private void Refresh()
    {
        var sections = CurrentSections;
        var status = latestResult is null ? TrayStatus.Neutral : TrayStatusCalculator.Calculate(latestResult, sections);
        trayIcon!.Icon = TrayIconFactory.Create(status, unreadComments.Count > 0);
        trayIcon.ToolTipText = sections is null ? "PrTray" : $"PrTray – {sections.Mine.Count} mine, {sections.ToReview.Count} til review{UnreadCommentsToolTip}";
        TrayMenuBuilder.Populate(menu, sections, latestResult, lastSnapshot?.FetchedAt, unreadComments, MenuActions);
        if (overviewWindow?.IsVisible == true)
            overviewWindow.ShowSections(sections, lastSnapshot?.FetchedAt, TimeProvider.System.GetUtcNow(), unreadComments);
    }

    private string UnreadCommentsToolTip => unreadComments.Count == 0 ? "" : $", {unreadComments.Count} med nye kommentarer";

    private void ShowOverview()
    {
        overviewWindow ??= new OverviewWindow(OpenUrl);
        overviewWindow.ShowSections(CurrentSections, lastSnapshot?.FetchedAt, TimeProvider.System.GetUtcNow(), unreadComments);
        overviewWindow.Show();
        overviewWindow.Activate();
    }

    private void ShowSettings()
    {
        if (settingsWindow is null)
        {
            settingsWindow = new SettingsWindow(
                config,
                SaveConfig,
                repository => ghClient.CheckRepositoryAsync(config, repository, CancellationToken.None),
                sound => notifier.Show(NotificationTexts.SoundPreview, sound));
            settingsWindow.Closed += (_, _) => settingsWindow = null;
        }
        settingsWindow.Show();
        settingsWindow.Activate();
    }

    private void SaveConfig(PrTrayConfig updatedConfig)
    {
        config = updatedConfig;
        configStore.Save(config);
        textStyles.Apply(Resources, config.TextSize);
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
