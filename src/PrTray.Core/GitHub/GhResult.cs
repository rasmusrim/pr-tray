using PrTray.Core.Models;

namespace PrTray.Core.GitHub;

public abstract record GhResult
{
    public sealed record Success(PrSnapshot Snapshot) : GhResult;

    public sealed record NotInstalled : GhResult;

    public sealed record NotAuthenticated : GhResult;

    public sealed record Failed(string Message) : GhResult;
}
