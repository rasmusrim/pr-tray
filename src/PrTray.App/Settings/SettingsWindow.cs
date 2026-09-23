using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using PrTray.App.Styling;
using PrTray.Core.Presentation;

namespace PrTray.App.Settings;

public sealed class SettingsWindow : Window
{
    private readonly List<string> repositories;
    private readonly Action<IReadOnlyList<string>> saveRepositories;
    private readonly StackPanel repositoryRows = new() { Spacing = 4 };
    private readonly TextBox newRepositoryInput = new() { PlaceholderText = "eier/repo eller GitHub-lenke" };
    private readonly TextBlock validationMessage = new() { Foreground = new SolidColorBrush(Color.Parse("#DA3633")), IsVisible = false };

    public SettingsWindow(IReadOnlyList<string> currentRepositories, Action<IReadOnlyList<string>> saveRepositories)
    {
        repositories = currentRepositories.ToList();
        this.saveRepositories = saveRepositories;
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

    private void AddRepository()
    {
        var candidate = RepositoryInput.Normalize(newRepositoryInput.Text ?? "");
        var problem = RepositoryInput.Problem(repositories, candidate);
        validationMessage.Text = problem;
        validationMessage.IsVisible = problem is not null;
        if (problem is not null)
            return;
        repositories.Add(candidate);
        newRepositoryInput.Text = "";
        RenderRepositories();
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
