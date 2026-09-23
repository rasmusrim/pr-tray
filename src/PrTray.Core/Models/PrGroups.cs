namespace PrTray.Core.Models;

[Flags]
public enum PrGroups
{
    None = 0,
    Mine = 1,
    ReviewRequested = 2,
    ReviewedByMe = 4,
    Watched = 8,
}
