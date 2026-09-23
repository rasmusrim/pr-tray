namespace PrTray.Core.Detection;

public sealed record DetectionResult(IReadOnlyList<PrEvent> Events, IReadOnlySet<string> SeenKeys);
