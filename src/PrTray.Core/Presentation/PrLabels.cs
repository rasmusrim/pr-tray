using PrTray.Core.GitHub;
using PrTray.Core.Models;

namespace PrTray.Core.Presentation;

public static class PrLabels
{
    public const int MaxTitleLength = 60;

    public static string StatusEmoji(PullRequest pullRequest) => pullRequest switch
    {
        { State: PrState.Merged } => "🟣",
        { IsDraft: true } => "📝",
        { ReviewDecision: ReviewDecision.ChangesRequested } => "❌",
        { ReviewDecision: ReviewDecision.Approved } => "✅",
        _ => "⏳",
    };

    public static string MenuLabel(PullRequest pullRequest) =>
        EscapeAccessKeys($"{StatusEmoji(pullRequest)} {pullRequest.DisplayName}  {Truncate(pullRequest.Title, MaxTitleLength)}");

    public static string? Problem(GhResult result) => result switch
    {
        GhResult.NotInstalled => "gh ikke funnet – installer GitHub CLI",
        GhResult.NotAuthenticated => "gh ikke innlogget – kjør gh auth login",
        GhResult.Failed failed => $"Henting feilet: {Truncate(failed.Message, MaxTitleLength)}",
        _ => null,
    };

    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    public static string EscapeAccessKeys(string text) => text.Replace("_", "__");
}
