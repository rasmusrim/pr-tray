namespace PrTray.Core.Presentation;

public static class RelativeAge
{
    public static string Format(TimeSpan age) => age switch
    {
        { TotalMinutes: < 1 } => "nå",
        { TotalHours: < 1 } => $"{(int)age.TotalMinutes} min",
        { TotalDays: < 1 } => $"{(int)age.TotalHours} t",
        _ => $"{(int)age.TotalDays} d",
    };
}
