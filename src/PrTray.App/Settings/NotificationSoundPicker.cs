using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using PrTray.Core.Presentation;

namespace PrTray.App.Settings;

public sealed class NotificationSoundPicker : StackPanel
{
    private readonly ComboBox soundInput = new()
    {
        ItemsSource = Enum.GetValues<NotificationSound>(),
        ItemTemplate = new FuncDataTemplate<NotificationSound>((sound, _) => new TextBlock { Text = NotificationSoundLabels.For(sound) }),
        MinWidth = 180,
    };
    private readonly Button chooseFileButton = new() { Content = "Velg fil…" };
    private readonly Button previewButton = new() { Content = "Spill av" };
    private readonly TextBlock fileName = new() { Opacity = 0.65, TextTrimming = TextTrimming.CharacterEllipsis };
    private string? customSoundFile;

    public NotificationSoundPicker(NotificationSoundSelection current, Action<NotificationSoundSelection> preview)
    {
        Spacing = 6;
        soundInput.SelectedItem = current.Sound;
        customSoundFile = current.CustomSoundFile;
        soundInput.SelectionChanged += (_, _) => Render();
        chooseFileButton.Click += (_, _) => _ = ChooseFileAsync();
        previewButton.Click += (_, _) => preview(Selection);
        Children.Add(new TextBlock { Text = "Lyd ved varsler" });
        Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children = { soundInput, chooseFileButton, previewButton },
        });
        Children.Add(fileName);
        Render();
    }

    public NotificationSoundSelection Selection =>
        SelectedSound == NotificationSound.Custom && customSoundFile is null
            ? new NotificationSoundSelection(NotificationSound.Message, null)
            : new NotificationSoundSelection(SelectedSound, customSoundFile);

    private NotificationSound SelectedSound => soundInput.SelectedItem as NotificationSound? ?? NotificationSound.Message;

    private async Task ChooseFileAsync()
    {
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storageProvider is null)
            return;
        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Velg lydfil",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Lydfiler") { Patterns = SupportedSoundFilePatterns }],
        });
        var chosenPath = files.Count == 0 ? null : files[0].TryGetLocalPath();
        if (chosenPath is null)
            return;
        customSoundFile = chosenPath;
        Render();
    }

    private void Render()
    {
        var isCustom = SelectedSound == NotificationSound.Custom;
        chooseFileButton.IsVisible = isCustom;
        previewButton.IsEnabled = SelectedSound != NotificationSound.None && (!isCustom || customSoundFile is not null);
        fileName.IsVisible = isCustom;
        fileName.Text = customSoundFile is null ? SupportedFormatsHint : Path.GetFileName(customSoundFile);
    }

    private static string[] SupportedSoundFilePatterns =>
        OperatingSystem.IsWindows() ? ["*.wav"]
        : OperatingSystem.IsMacOS() ? ["*.wav", "*.mp3", "*.m4a", "*.aiff", "*.caf"]
        : ["*.wav", "*.ogg", "*.oga"];

    private static string SupportedFormatsHint =>
        "Ingen fil valgt. Støttede formater: " + string.Join(", ", SupportedSoundFilePatterns.Select(pattern => pattern.TrimStart('*')));
}
