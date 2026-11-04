using PrTray.Core.Presentation;
using PrTray.Core.Storage;
using static PrTray.Core.Tests.TestPullRequests;

namespace PrTray.Core.Tests.Storage;

public sealed class UnreadCommentsStoreTests : IDisposable
{
    private readonly TemporaryDirectory temporaryDirectory = new();

    private string FilePath => temporaryDirectory.FilePath("unread-comments.json");

    private UnreadCommentsStore Store => new(FilePath);

    public void Dispose() => temporaryDirectory.Dispose();

    private UnreadComments LoadFrom(string json)
    {
        Directory.CreateDirectory(temporaryDirectory.DirectoryPath);
        File.WriteAllText(FilePath, json);
        return Store.Load();
    }

    [Fact]
    public void Missing_file_means_nothing_is_unread()
    {
        Assert.Equal(0, Store.Load().Count);
    }

    [Fact]
    public void Saved_entries_round_trip()
    {
        var entry = new UnreadComment("PR_1", "https://github.com/acme/widgets/pull/70", "https://github.com/acme/widgets/pull/70#issuecomment-1", Now);

        Store.Save(new UnreadComments([entry]));

        Assert.Equal(entry, Assert.Single(Store.Load().Entries));
    }

    [Fact]
    public void Corrupt_file_means_nothing_is_unread()
    {
        Assert.Equal(0, LoadFrom("{ not json").Count);
    }

    [Fact]
    public void Incomplete_entries_are_skipped()
    {
        var unread = LoadFrom("""[null, {}, { "PullRequestId": "PR_1", "PullRequestUrl": "https://github.com/acme/widgets/pull/70", "FirstCommentUrl": "https://github.com/acme/widgets/pull/70#issuecomment-1" }]""");

        Assert.Equal("PR_1", Assert.Single(unread.Entries).PullRequestId);
    }
}
