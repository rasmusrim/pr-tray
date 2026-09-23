namespace PrTray.Core.Models;

public sealed record PrSnapshot(string ViewerLogin, IReadOnlyList<PullRequest> PullRequests, DateTimeOffset FetchedAt);
