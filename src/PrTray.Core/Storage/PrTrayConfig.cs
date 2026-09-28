using PrTray.Core.Presentation;

namespace PrTray.Core.Storage;

public sealed record PrTrayConfig(IReadOnlyList<string> Repositories, int PollIntervalSeconds, string GhPath,
    NotificationSound NotificationSound,
    string? CustomSoundFile)
{
    public static PrTrayConfig Default { get; } = new([], 120, "gh", NotificationSound.Message, CustomSoundFile: null);
}
