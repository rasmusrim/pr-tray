using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PrTray.App.Styling;
using PrTray.Core.GitHub;
using PrTray.Core.Presentation;

namespace PrTray.App.Settings;

public sealed class SettingsWindow : Window
{
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#DA3633"));

    private readonly List<string> repositories;
    private readonly Action<IReadOnlyList<string>> saveRepositories;
    private readonly Func<string, Task<RepositoryCheck>> checkRepository;
    private readonly StackPanel repositoryRows = new() { Spacing = 4 };
    private readonly TextBox newRepositoryInput = new() { PlaceholderText = "eier/repo eller GitHub-lenke" };
    private readonly TextBlock validationMessage = new() { IsVisible = false, TextWrapping = TextWrapping.Wrap };

    public SettingsWindow(
        IReadOnlyList<string> currentRepositories,
        Action<IReadOnlyList<string>> saveRepositories,
        Func<string, Task<RepositoryCheck>> checkRepository)
    {
        repositories = currentRepositories.ToList();
        this.saveRepositories = saveRepositories;
        this.checkRepository = checkRepository;
        Title = "PrTray – innstillinger";
        Width = 540;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        newRepositoryInput.KeyDown += (_, keyArguments) =>
        {
            if (keyArguments.Key == Key.Enter)
                AddRepository();
        };

        var buttons = SettingsLayout.ButtonRow(("Avbryt", false, Close), ("Lagre", true, SaveAndClose));
        DockPanel.SetDock(buttons, Dock.Bottom);
        var header = SettingsLayout.Header(newRepositoryInput, AddRepository, validationMessage);
        DockPanel.SetDock(header, Dock.Top);
        Content = new DockPanel
        {
            Margin = new Thickness(20),
            Children = { buttons, header, new ScrollViewer { Content = repositoryRows, Margin = new Thickness(0, 12) } },
        };
        RenderRepositories();
    }

    private void AddRepository() => _ = AddRepositoryAsync();

    private async Task AddRepositoryAsync()
    {
        if (!newRepositoryInput.IsEnabled)
            return;
        var candidate = RepositoryInput.Normalize(newRepositoryInput.Text ?? "");
        var problem = RepositoryInput.Problem(repositories, candidate);
        if (problem is not null)
        {
            ShowMessage(problem, isError: true);
            return;
        }
        newRepositoryInput.IsEnabled = false;
        ShowMessage("Sjekker at repoet finnes…", isError: false);
        var check = await checkRepository(candidate);
        newRepositoryInput.IsEnabled = true;
        if (check == RepositoryCheck.NotFound)
        {
            ShowMessage("Fant ikke repoet på GitHub, eller du har ikke tilgang til det.", isError: true);
            return;
        }
        validationMessage.IsVisible = false;
        repositories.Add(candidate);
        newRepositoryInput.Text = "";
        RenderRepositories();
    }

    private void ShowMessage(string text, bool isError)
    {
        validationMessage.Text = text;
        validationMessage.Foreground = isError ? ErrorBrush : null;
        validationMessage.Opacity = isError ? 1 : 0.65;
        validationMessage.IsVisible = true;
    }

    private void RemoveRepository(string repository)
    {
        repositories.Remove(repository);
        RenderRepositories();
    }

    private void SaveAndClose()
    {
        saveRepositories(repositories.ToList());
        Close();
    }

    private void RenderRepositories()
    {
        repositoryRows.Children.Clear();
        if (repositories.Count == 0)
            repositoryRows.Children.Add(new TextBlock
            {
                Text = "Ingen repoer valgt – alle repoer vises, men du får ikke varsel om nye PR-er.",
                Opacity = 0.65,
                TextWrapping = TextWrapping.Wrap,
            });
        foreach (var repository in repositories)
            repositoryRows.Children.Add(RepositoryRow(repository));
    }

    private Border RepositoryRow(string repository)
    {
        var removeButton = new Button { Content = "Fjern", VerticalAlignment = VerticalAlignment.Center };
        removeButton.Click += (_, _) => RemoveRepository(repository);
        Grid.SetColumn(removeButton, 1);
        return new Border
        {
            Background = StatusColors.CardBackground,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 6, 6, 6),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Children = { new TextBlock { Text = repository, FontSize = 14, VerticalAlignment = VerticalAlignment.Center }, removeButton },
            },
        };
    }
}
