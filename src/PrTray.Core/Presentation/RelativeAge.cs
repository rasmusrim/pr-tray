namespace PrTray.Core.Presentation;

public static class RelativeAge
{
    private const double DaysPerMonth = 30.44;
    private const double DaysPerYear = 365.25;

    public static string Format(TimeSpan age) => age switch
    {
        { TotalMinutes: < 1 } => "nå",
        { TotalHours: < 1 } => $"{(int)age.TotalMinutes} min",
        { TotalDays: < 1 } => $"{(int)age.TotalHours} t",
        { TotalDays: < DaysPerMonth } => $"{(int)age.TotalDays} d",
        { TotalDays: < DaysPerYear } => $"{(int)(age.TotalDays / DaysPerMonth)} mnd",
        _ => $"{(int)(age.TotalDays / DaysPerYear)} år",
    };
}
