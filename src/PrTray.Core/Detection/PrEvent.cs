using PrTray.Core.Models;

namespace PrTray.Core.Detection;

public sealed record PrEvent(PrEventKind Kind, PullRequest PullRequest, string? ActorLogin)
{
    public IReadOnlyList<Comment> Comments { get; init; } = [];
}
