namespace PrTray.Core.Presentation;

public static class TextSizes
{
    public static double Scale(TextSize size) => size switch
    {
        TextSize.Normal => 1.0,
        TextSize.Large => 1.15,
        TextSize.Larger => 1.3,
        TextSize.Largest => 1.5,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    public static string Label(TextSize size) => size switch
    {
        TextSize.Normal => "Normal",
        TextSize.Large => "Stor",
        TextSize.Larger => "Større",
        TextSize.Largest => "Størst",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    } + $" ({Scale(size) * 100:0} %)";
}
