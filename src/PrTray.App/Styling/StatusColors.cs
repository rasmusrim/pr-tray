using Avalonia.Media;
using PrTray.Core.Models;

namespace PrTray.App.Styling;

public static class StatusColors
{
    private const double SubtleOpacity = 0.18;

    public static readonly IBrush CardBackground = new SolidColorBrush(Color.Parse("#808080"), 0.08);

    public static Color For(PullRequest pullRequest) => pullRequest switch
    {
        { State: PrState.Merged } => Color.Parse("#8957E5"),
        { IsDraft: true } => Color.Parse("#6E7681"),
        { ReviewDecision: ReviewDecision.ChangesRequested } => Color.Parse("#DA3633"),
        { ReviewDecision: ReviewDecision.Approved } => Color.Parse("#2EA043"),
        _ => Color.Parse("#D29922"),
    };

    public static IBrush Subtle(Color color) => new SolidColorBrush(color, SubtleOpacity);
}
