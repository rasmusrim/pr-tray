namespace PrTray.Core.Storage;

public sealed record PrTrayConfig(IReadOnlyList<string> Repositories, int PollIntervalSeconds, string GhPath)
{
    public static PrTrayConfig Default { get; } = new(["acme/widgets"], 120, "gh");
}
