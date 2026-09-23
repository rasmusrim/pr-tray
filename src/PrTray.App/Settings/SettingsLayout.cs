using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace PrTray.App.Settings;

public static class SettingsLayout
{
    public static StackPanel Header(TextBox newRepositoryInput, Action addRepository, TextBlock validationMessage)
    {
        var addButton = new Button { Content = "Legg til", VerticalAlignment = VerticalAlignment.Stretch };
        addButton.Click += (_, _) => addRepository();
        Grid.SetColumn(addButton, 1);
        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Repoer", FontSize = 22, FontWeight = FontWeight.SemiBold },
                new TextBlock
                {
                    Text = "Bare PR-er i disse repoene vises og varsles. Du får også varsel om nye PR-er, godkjenninger og merger i dem.",
                    Opacity = 0.65,
                    TextWrapping = TextWrapping.Wrap,
                },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    ColumnSpacing = 8,
                    Margin = new Thickness(0, 8, 0, 0),
                    Children = { newRepositoryInput, addButton },
                },
                validationMessage,
            },
        };
    }

    public static StackPanel ButtonRow(params (string Label, bool IsPrimary, Action OnClick)[] buttons)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var buttonSpecification in buttons)
        {
            var button = new Button { Content = buttonSpecification.Label, MinWidth = 90, HorizontalContentAlignment = HorizontalAlignment.Center };
            if (buttonSpecification.IsPrimary)
                button.Classes.Add("accent");
            button.Click += (_, _) => buttonSpecification.OnClick();
            row.Children.Add(button);
        }
        return row;
    }
}
