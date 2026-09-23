namespace PrTray.Core.Detection;

public sealed record SeenState(IReadOnlySet<string> Keys, bool IsFirstRun)
{
    public static SeenState FirstRun => new(new HashSet<string>(), IsFirstRun: true);
}
