using PrTray.Core.Detection;
using PrTray.Core.GitHub;

namespace PrTray.Core.Polling;

public sealed record PollOutcome(GhResult Result, IReadOnlyList<PrEvent> Events, DateTimeOffset CompletedAt);
