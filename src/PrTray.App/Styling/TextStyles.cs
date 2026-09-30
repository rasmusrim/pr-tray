using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using PrTray.Core.Presentation;

namespace PrTray.App.Styling;

// Fluent sizes every window and control from ControlContentThemeFontSize, so overriding it scales all
// default text. Text that is deliberately smaller or larger uses these classes instead of a fixed FontSize.
public sealed class TextStyles : Styles
{
    public const string Caption = "caption";
    public const string Heading = "heading";
    public const string Title = "title";

    private const double BodyFontSize = 14;

    private static readonly (string ClassName, double FontSize)[] Roles = [(Caption, 12), (Heading, 15), (Title, 22)];

    public void Apply(IResourceDictionary resources, TextSize size)
    {
        var scale = TextSizes.Scale(size);
        resources["ControlContentThemeFontSize"] = BodyFontSize * scale;
        Clear();
        foreach (var (className, fontSize) in Roles)
            Add(new Style(selector => selector.OfType<TextBlock>().Class(className))
            {
                Setters = { new Setter(TextBlock.FontSizeProperty, fontSize * scale) },
            });
    }
}
