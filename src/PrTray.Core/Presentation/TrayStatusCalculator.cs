using PrTray.Core.GitHub;
using PrTray.Core.Models;

namespace PrTray.Core.Presentation;

public static class TrayStatusCalculator
{
    public static TrayStatus Calculate(GhResult latestResult, PrSections? sections)
    {
        if (latestResult is not GhResult.Success || sections is null)
            return TrayStatus.Error;
        if (sections.ToReview.Count > 0 || sections.Mine.Any(pullRequest => pullRequest.ReviewDecision == ReviewDecision.ChangesRequested))
            return TrayStatus.NeedsAttention;
        if (sections.Mine.Any(pullRequest => pullRequest.ReviewDecision == ReviewDecision.Approved))
            return TrayStatus.Approved;
        return TrayStatus.Neutral;
    }
}
