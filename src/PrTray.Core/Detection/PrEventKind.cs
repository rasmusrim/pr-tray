namespace PrTray.Core.Detection;

public enum PrEventKind
{
    Opened,
    NewCommitsSinceUnapprovedReview,
    Approved,
    ChangesRequested,
    ReviewRequested,
    ReadyForReview,
    Merged,
}
